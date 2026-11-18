using System.Text;
using System.Xml.Linq;
using CdaCrImg.Analyse;
using CdaCrImg.Referentiel;
using CdaCrImg.Serialization;
using Xunit.Abstractions;

namespace CdaCrImg.Tests;

/// <summary>Analyse d'un CDA existant : champs présents, absents (obligatoires ou facultatifs), non gérés.</summary>
public class CdaAnalyseurTests(ITestOutputHelper output)
{
    private static readonly XNamespace Hl7 = CdaNamespaces.Hl7;

    [Fact]
    public void CompleteDocument_HasEveryCallerFieldAndNothingUnmanaged()
    {
        var analyse = CdaAnalyseur.Analyser(CrImgWriter.Write(SampleReports.Complete()));

        Assert.True(analyse.EstComplet, string.Join("\n", analyse.Anomalies));
        Assert.Empty(analyse.NonGeres);
        Assert.False(analyse.CorpsStructure);
        // Aucun champ perdu entre l'écriture et l'analyse.
        Assert.All(ReferentielChamps.Champs, c => Assert.Contains(analyse.Presents, a => a.Champ == c));
    }

    [Fact]
    public void Values_AreReadWithIndexedPaths()
    {
        var analyse = CdaAnalyseur.Analyser(CrImgWriter.Write(SampleReports.Complete()));
        string? Valeur(string chemin) => analyse.Champs.Single(c => c.Chemin == chemin).Valeur;

        Assert.Equal("1.2.250.1.213.1.4.10^279035121518989", Valeur("Patient.Ins"));
        Assert.Equal("PAT-TROIS", Valeur("Patient.NomNaissance"));
        Assert.Equal("19790328", Valeur("Patient.DateNaissance"));
        Assert.Equal("1.2.250.1.71.4.2.1^801234560801", Valeur("Auteurs[0].Professionnel.Id"));
        Assert.Equal("G15_10/SM44 — Médecin - Radio-diagnostic (SM)", Valeur("Auteurs[0].Professionnel.Profession"));
        Assert.Equal("M Jacques BIDEAULT DR", Valeur("Auteurs[0].Professionnel.Nom"));
        Assert.Equal("1.2.250.1.925.994044.27.123.1876361", Valeur("Actes[1].StudyInstanceUid"));
        Assert.Equal("CT — Tomodensitométrie", Valeur("Actes[0].Modalites[0]"));
        Assert.Equal("nullFlavor=UNK", Valeur("Demandes[1].NumeroDemande"));
        Assert.Equal($"application/pdf encapsulé en base64 ({SampleReports.AnsPdf.Length} octets)", Valeur("Corps"));
        Assert.Equal("/ClinicalDocument/documentationOf[2]/serviceEvent/id",
            analyse.Champs.Single(c => c.Chemin == "Actes[1].StudyInstanceUid").Emplacement);
    }

    [Fact]
    public void MinimalDocument_ListsMissingOptionalFields()
    {
        var analyse = CdaAnalyseur.Analyser(CrImgWriter.Write(SampleReports.Minimal()));

        Assert.True(analyse.EstComplet, string.Join("\n", analyse.Anomalies));
        Assert.Equal(StatutChamp.FacultatifAbsent, Statut(analyse, "Patient.NomUtilise"));
        Assert.Equal(StatutChamp.FacultatifAbsent, Statut(analyse, "MedecinsDemandeurs"));
        // Les champs d'un groupe absent ne sont pas listés.
        Assert.DoesNotContain(analyse.Champs, c => c.Chemin.StartsWith("MedecinsDemandeurs[", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingRequiredField_IsReported()
    {
        var cda = CrImgWriter.Write(SampleReports.Complete());
        cda.Descendants(Hl7 + "birthTime").Single().Remove();
        cda.Root!.Element(Hl7 + "author")!.Descendants(Hl7 + "family").Single().Remove();

        var analyse = CdaAnalyseur.Analyser(cda);

        Assert.False(analyse.EstComplet);
        Assert.Equal(new[] { "Patient.DateNaissance", "Auteurs[0].Professionnel.Nom.Family" },
            analyse.Anomalies.Select(a => a.Chemin));
        Assert.All(analyse.Anomalies, a => Assert.Equal(StatutChamp.ObligatoireAbsent, a.Statut));
    }

    [Fact]
    public void ExtraOccurrence_IsReported()
    {
        var cda = CrImgWriter.Write(SampleReports.Minimal());
        var titre = cda.Root!.Element(Hl7 + "title")!;
        titre.AddAfterSelf(new XElement(titre));

        var anomalie = Assert.Single(CdaAnalyseur.Analyser(cda).Anomalies);

        Assert.Equal(StatutChamp.EnTrop, anomalie.Statut);
        Assert.Equal("/ClinicalDocument/title[2]", anomalie.Emplacement);
    }

    [Fact]
    public void AnsLevel1Example_IsCompleteAndListsUnmanagedElements()
    {
        var analyse = CdaAnalyseur.Analyser(RepoPaths.CrImgLevel1Example);
        Afficher(analyse);

        Assert.True(analyse.EstComplet, string.Join("\n", analyse.Anomalies));
        Assert.Contains(analyse.NonGeres, e => e.Emplacement.StartsWith("/ClinicalDocument/informant", StringComparison.Ordinal));
        Assert.Contains(analyse.NonGeres, e => e.Emplacement == "/ClinicalDocument/authenticator");
        Assert.Contains(analyse.NonGeres, e => e.Emplacement == "/ClinicalDocument/participant");
        Assert.All(analyse.Presents, c => Assert.NotNull(c.Ligne));
    }

    [Fact]
    public void Level3Example_AnalysesHeaderAndFlagsStructuredBody()
    {
        var analyse = CdaAnalyseur.Analyser(RepoPaths.CrImgReferenceExample);

        Assert.True(analyse.CorpsStructure);
        Assert.Equal(StatutChamp.HorsPerimetre, Statut(analyse, "Corps"));
        Assert.Contains(analyse.NonGeres, e => e.Emplacement == "/ClinicalDocument/component");
        Assert.Equal(StatutChamp.Present, Statut(analyse, "Patient.Ins"));
    }

    [Fact]
    public void Stream_IsAnalysedWithLineNumbers()
    {
        var xml = CrImgWriter.WriteToString(SampleReports.Minimal());

        var analyse = CdaAnalyseur.Analyser(new MemoryStream(Encoding.UTF8.GetBytes(xml)));

        var titre = analyse.Champs.Single(c => c.Chemin == "Titre");
        Assert.Equal(xml.Split('\n').ToList().FindIndex(l => l.Contains("<title>")) + 1, titre.Ligne);
    }

    [Theory]
    [InlineData("<ClinicalDocument xmlns=\"urn:hl7-org:v3\"><code code=\"11488-4\"/></ClinicalDocument>", "compte rendu d'imagerie")]
    [InlineData("<ClinicalDocument xmlns=\"urn:hl7-org:v3\"/>", "code du document absent")]
    [InlineData("<html/>", "pas un CDA R2")]
    [InlineData("<ClinicalDocument", "XML mal formé")]
    [InlineData("<!DOCTYPE ClinicalDocument [<!ENTITY a \"aaaaaaaaaa\"><!ENTITY b \"&a;&a;&a;&a;&a;&a;&a;&a;&a;&a;\">]>"
        + "<ClinicalDocument xmlns=\"urn:hl7-org:v3\"><title>&b;</title></ClinicalDocument>", "XML mal formé")]
    public void InvalidDocument_IsRejected(string xml, string message)
    {
        var e = Assert.Throws<CdaAnalyseException>(() => CdaAnalyseur.Analyser(new MemoryStream(Encoding.UTF8.GetBytes(xml))));

        Assert.Contains(message, e.Message);
    }

    private static StatutChamp Statut(AnalyseCda analyse, string chemin) => analyse.Champs.Single(c => c.Chemin == chemin).Statut;

    private void Afficher(AnalyseCda analyse)
    {
        foreach (var champ in analyse.Champs) output.WriteLine(champ.ToString());
        foreach (var element in analyse.NonGeres) output.WriteLine("Non géré : " + element);
    }
}
