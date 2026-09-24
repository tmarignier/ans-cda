using System.Xml.Linq;
using CdaCrImg.Model;
using CdaCrImg.Model.Hl7;
using CdaCrImg.Serialization;

namespace CdaCrImg.Tests;

/// <summary>
/// Non-régression : l'exemple ANS niveau 1 (ExemplesCDA/IMG_CR_IMG_2024.01_CDA-R2-Niveau-1.xml) est
/// reconstruit depuis le modèle CdaCrImg, puis comparé structurellement à l'original (éléments, attributs,
/// textes ; commentaires, espaces et ordre des attributs ignorés).
/// </summary>
public class AnsExampleNonRegressionTests
{
    private static readonly XNamespace V3 = CdaNamespaces.Hl7;

    /// <summary>
    /// Éléments de l'exemple ANS que la librairie ne produit pas (reportés, voir la roadmap) : retirés de
    /// l'original avant comparaison. Toute autre différence fait échouer le test.
    /// </summary>
    private static readonly (string Description, Func<XDocument, IEnumerable<XElement>> Select)[] NonGeres =
    {
        ("informant (personne à prévenir, personne de confiance)", d => d.Root!.Elements(V3 + "informant")),
        ("authenticator", d => d.Root!.Elements(V3 + "authenticator")),
        ("participant typeCode=INF (médecin traitant)", d => d.Root!.Elements(V3 + "participant").Where(p => (string?)p.Attribute("typeCode") == "INF")),
        ("patient/guardian (représentant du patient)", d => d.Descendants(V3 + "guardian")),
    };

    /// <summary>
    /// Incohérence de l'exemple ANS : la 3e translation du code du document (24979-7 « CT Thoracic spine W
    /// contrast IV ») ne reprend pas le code de l'acte documenté correspondant (24978-9 « CT Thoracic spine »),
    /// alors que la STD impose une translation par acte avec son code LOINC. La librairie dérive les
    /// translations des actes : l'original est corrigé avant comparaison.
    /// </summary>
    private static XElement TroisiemeTranslation(XDocument d) =>
        d.Root!.Element(V3 + "code")!.Elements(V3 + "translation").ElementAt(2);

    [Fact]
    public void RebuiltAnsExample_MatchesOriginal()
    {
        var original = XDocument.Load(RepoPaths.CrImgLevel1Example);
        foreach (var (_, select) in NonGeres) select(original).ToList().ForEach(e => e.Remove());
        TroisiemeTranslation(original).SetAttributeValue("code", "24978-9");
        TroisiemeTranslation(original).SetAttributeValue("displayName", "CT Thoracic spine");

        var rebuilt = CrImgWriter.Write(AnsLevel1Example());

        var differences = new List<string>();
        Compare(original.Root!, rebuilt.Root!, "/ClinicalDocument", differences);
        Assert.True(differences.Count == 0, "Différences avec l'exemple ANS :\n" + string.Join("\n", differences.Take(30)));
    }

    [Fact]
    public void NonGeresAndAnomaly_AreStillPresentInAnsExample()
    {
        // Garde-fous : si l'ANS modifie son exemple (élément retiré, incohérence corrigée), ce test doit être mis à jour.
        var original = XDocument.Load(RepoPaths.CrImgLevel1Example);
        Assert.All(NonGeres, n => Assert.NotEmpty(n.Select(original)));
        Assert.Equal("24979-7", (string?)TroisiemeTranslation(original).Attribute("code"));
        Assert.Equal("24978-9", (string?)original.Root!.Elements(V3 + "documentationOf").ElementAt(2)
            .Element(V3 + "serviceEvent")!.Element(V3 + "code")!.Attribute("code"));
    }

    /// <summary>Le compte rendu de l'exemple ANS niveau 1, décrit avec le modèle CdaCrImg.</summary>
    private static CompteRenduImagerie AnsLevel1Example()
    {
        var hiver = TimeSpan.FromHours(1);
        var creation = new DateTimeOffset(2021, 1, 8, 11, 17, 0, hiver);
        var debut = new DateTimeOffset(2021, 1, 8, 10, 25, 0, hiver);
        Address AdresseCentre() => new() { HouseNumber = "12", StreetName = "Rue Ambroise", PostalCode = "75010", City = "PARIS" };

        var centre = new Organisation
        {
            Id = Identifier.FromFiness("750803447"),
            Nom = "Centre de radiologie Ambroise",
            SecteurActivite = new Code("AMBULATOIRE", CodeSystems.SecteurActivite, "Ambulatoire"),
        };
        centre.Telecoms.Add(Telecom.Phone("0146000000"));
        centre.Adresses.Add(AdresseCentre());

        var radiologue = new Professionnel
        {
            Id = Identifier.FromRpps("01234560801"),
            Profession = Code.ProfessionSavoirFaire("G15_10/SM44", "Médecin - Radio-diagnostic (SM)"),
            Nom = new PersonName("BIDEAULT", "Jacques", prefix: "M", suffix: "DR"),
            Organisation = centre,
        };
        radiologue.Adresses.Add(AdresseCentre());
        radiologue.Telecoms.Add(Telecom.Phone("0146000000", "WP"));

        var patient = new Patient
        {
            Ins = new Identifier(IdentifierRoots.InsNirTest, "279035121518989"),
            NomNaissance = "PAT-TROIS",
            PrenomsNaissance = "DOMINIQUE MARIE-LOUISE",
            PremierPrenomNaissance = "DOMINIQUE",
            NomUtilise = "PAT-TROIS",
            PrenomUtilise = "DOMINIQUE",
            Sexe = Sexe.Feminin,
            DateNaissance = new DateTime(1979, 3, 28),
            LieuNaissanceCog = "51215",
            LieuNaissanceCommune = "DOMPREMY",
        };
        patient.AutresIdentifiants.Add(new Identifier("1.2.3.4.567.8.9.10", "1234567890121"));
        patient.Adresses.Add(new Address
        {
            HouseNumber = "28", StreetName = "Avenue de Breteuil", UnitId = "Escalier A", PostalCode = "75007", City = "PARIS", Country = "FRANCE",
        });
        patient.Telecoms.Add(Telecom.Phone("0144534551", "H"));
        patient.Telecoms.Add(Telecom.Phone("0647151010", "MC"));
        patient.Telecoms.Add(Telecom.Email("279035121518989@patient.mssante.fr"));

        var cr = new CompteRenduImagerie
        {
            Id = new Identifier("1.2.250.1.213.1.1.1.45.2024.2.1"),
            SetId = new Identifier("1.2.250.1.213.1.1.1.45.2024.2"),
            NumeroVersion = 2,
            DocumentRemplace = new Identifier("90E1C8EC-F951-4B26-A305-A34848818DD6"),
            Titre = "CR d’imagerie médicale - Scanner Tête + Cou + Thorax avec injection",
            DateCreation = creation,
            Patient = patient,
            Custodian = centre,
            SignataireLegal = new Signature(radiologue, creation),
            Depistage = true,
            PriseEnCharge = new PriseEnCharge
            {
                Modalite = new Code("AMB", CodeSystems.Hl7ActCode, "Ambulatoire (hors établissement)"),
                Debut = debut,
                Fin = creation,
                Lieu = new LieuPriseEnCharge
                {
                    Id = Identifier.FromFiness("750803447"),
                    CadreExercice = new Code("SA08", CodeSystems.CadreExercice, "Cabinet de groupe"),
                    Nom = "Centre de radiologie Ambroise",
                    Adresse = AdresseCentre(),
                },
            },
            Corps = new CorpsPdf(SampleReports.AnsPdf),
        };
        cr.Auteurs.Add(new Auteur(radiologue, creation)
        {
            Fonction = new Code("ATTPHYS", CodeSystems.Hl7ParticipationFunction, "Référent - Responsable du patient dans la structure de soins"),
        });
        cr.Demandes.Add(new DemandeImagerie(
            new Identifier("1.2.250.1.748.12345678.12", "984375862"),
            new Identifier("1.2.250.1.925.994044.27", "105234751")));

        ActeImagerie Acte(string uid, string loinc, string loincLabel, string ccam, string ccamLabel, string region, string regionLabel)
        {
            var acte = new ActeImagerie
            {
                StudyInstanceUid = uid,
                Code = Code.Loinc(loinc, loincLabel),
                CodeCcam = Code.Ccam(ccam, ccamLabel),
                Debut = debut,
                Fin = creation,
                Executant = radiologue,
            };
            acte.Modalites.Add(Code.Dcm("CT", "Tomodensitométrie"));
            acte.RegionsAnatomiques.Add(Code.Snomed(region, regionLabel));
            return acte;
        }
        cr.Actes.Add(Acte("1.2.250.1.925.994044.27.123.1876350", "24727-0", "CT Head W contrast IV",
            "ACQH004", "Scanographie du crâne, de son contenu et du tronc, avec injection intraveineuse de produit de contraste",
            "774007", "structure de la tête et/ou du cou"));
        cr.Actes.Add(Acte("1.2.250.1.925.994044.27.123.1876351", "36235-0", "CT Neck W contrast IV",
            "LCQH001", "Scanographie des tissus mous du cou, avec injection intraveineuse de produit de contraste",
            "774007", "structure de la tête et/ou du cou"));
        cr.Actes.Add(Acte("1.2.250.1.925.994044.27.123.1876352", "24978-9", "CT Thoracic spine",
            "ZBQH001", "Scanographie du thorax, avec injection intraveineuse de produit de contraste",
            "67734004", "segment thoracique du tronc"));
        return cr;
    }

    /// <summary>Comparaison structurelle : nom, attributs (hors déclarations d'espaces de noms et xsi:schemaLocation), texte, enfants.</summary>
    private static void Compare(XElement expected, XElement actual, string path, List<string> differences)
    {
        if (expected.Name != actual.Name)
        {
            differences.Add($"{path} : élément <{expected.Name.LocalName}> attendu, <{actual.Name.LocalName}> obtenu");
            return;
        }

        var expectedAttributes = Attributes(expected);
        var actualAttributes = Attributes(actual);
        foreach (var name in expectedAttributes.Keys.Union(actualAttributes.Keys).OrderBy(n => n.ToString()))
        {
            expectedAttributes.TryGetValue(name, out var e);
            actualAttributes.TryGetValue(name, out var a);
            if (e != a) differences.Add($"{path}/@{name.LocalName} : « {e} » attendu, « {a} » obtenu");
        }

        var expectedChildren = expected.Elements().ToList();
        var actualChildren = actual.Elements().ToList();
        if (expectedChildren.Count == 0 && actualChildren.Count == 0 && Text(expected) != Text(actual))
            differences.Add($"{path} : texte « {Truncate(Text(expected))} » attendu, « {Truncate(Text(actual))} » obtenu");

        for (var i = 0; i < Math.Max(expectedChildren.Count, actualChildren.Count); i++)
        {
            if (i >= expectedChildren.Count) differences.Add($"{path} : élément <{actualChildren[i].Name.LocalName}> en trop");
            else if (i >= actualChildren.Count) differences.Add($"{path} : élément <{expectedChildren[i].Name.LocalName}> manquant");
            else Compare(expectedChildren[i], actualChildren[i], $"{path}/{expectedChildren[i].Name.LocalName}[{i}]", differences);
        }
    }

    private static Dictionary<XName, string> Attributes(XElement element) =>
        element.Attributes()
            .Where(a => !a.IsNamespaceDeclaration && a.Name != CdaNamespaces.Xsi + "schemaLocation")
            .ToDictionary(a => a.Name, a => a.Value);

    /// <summary>Texte normalisé (espaces ignorés dans le base64 du PDF, réduits ailleurs).</summary>
    private static string Text(XElement element) =>
        (string?)element.Attribute("representation") == "B64"
            ? string.Concat(element.Value.Where(c => !char.IsWhiteSpace(c)))
            : string.Join(" ", element.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Truncate(string value) => value.Length > 60 ? value[..60] + "…" : value;
}
