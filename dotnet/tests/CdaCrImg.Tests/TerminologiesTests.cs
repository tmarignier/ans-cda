using CdaCrImg.Model;
using CdaCrImg.Model.Hl7;
using CdaCrImg.Terminologies;
using CdaCrImg.Validation;

namespace CdaCrImg.Tests;

/// <summary>Jeux de valeurs embarqués et contrôle des codes par <see cref="CrImgValidator"/>.</summary>
public class TerminologiesTests
{
    private static IEnumerable<string> Paths(CompteRenduImagerie cr, CrImgValidationOptions? options = null) =>
        CrImgValidator.Validate(cr, options).Select(i => i.Path);

    [Fact]
    public void EmbeddedValueSets_AreLoadedFromAnsArtifacts()
    {
        var expected = new Dictionary<string, (string Oid, string Code, string System)>
        {
            [nameof(JeuxDeValeursCisis.ProfessionSavoirFaire)] = ("1.2.250.1.213.1.1.5.461", "G15_10/SM44", CodeSystems.ProfessionSavoirFaire),
            [nameof(JeuxDeValeursCisis.CadreExercice)] = ("1.2.250.1.213.1.1.5.466", "SA08", CodeSystems.CadreExercice),
            [nameof(JeuxDeValeursCisis.SecteurActivite)] = ("1.2.250.1.213.1.1.5.467", "AMBULATOIRE", CodeSystems.SecteurActivite),
            [nameof(JeuxDeValeursCisis.TypeRencontre)] = ("1.2.250.1.213.1.1.5.589", "AMB", CodeSystems.Hl7ActCode),
            [nameof(JeuxDeValeursCisis.Fonction)] = ("1.2.250.1.213.1.1.5.124", "ATTPHYS", CodeSystems.Hl7ParticipationFunction),
            [nameof(JeuxDeValeursCisis.Civilite)] = ("1.2.250.1.213.1.1.5.718", "MME", "1.2.250.1.213.1.6.1.69"),
            [nameof(JeuxDeValeursCisis.Titre)] = ("1.2.250.1.213.1.1.5.719", "DR", "1.2.250.1.213.1.6.1.11"),
            [nameof(JeuxDeValeursCisis.Confidentialite)] = ("2.16.840.1.113883.1.11.16926", "N", CodeSystems.Hl7Confidentiality),
            [nameof(JeuxDeValeursCisis.ModaliteAcquisition)] = ("1.2.250.1.213.1.1.5.618", "CT", CodeSystems.Dcm),
            [nameof(JeuxDeValeursCisis.RegionAnatomique)] = ("1.2.250.1.213.1.1.5.695", "774007", CodeSystems.SnomedCt),
        };
        foreach (var (name, (oid, code, system)) in expected)
        {
            var jdv = (JeuDeValeurs)typeof(JeuxDeValeursCisis).GetProperty(name)!.GetValue(null)!;
            Assert.Equal(oid, jdv.Oid);
            Assert.True(jdv.Contient(code, system), $"{name} : {code} attendu.");
        }
        Assert.Equal(expected.Count, JeuxDeValeursCisis.Tous.Count);
        Assert.Equal(390, JeuxDeValeursCisis.ProfessionSavoirFaire.Concepts.Count);
    }

    [Fact]
    public void Charger_ReadsSvsFormat()
    {
        var jdv = JeuDeValeurs.Charger(Path.Combine(RepoPaths.Root, "jeuxDeValeurs", "jdv-code-document-imagerie-cisis.xml"));

        Assert.Equal("1.2.250.1.213.1.1.5.687", jdv.Oid);
        Assert.True(jdv.Concepts.Count > 6000);
        Assert.Equal("CT tête avec contraste IV", jdv.Trouver("24727-0", CodeSystems.Loinc)?.DisplayName);
    }

    [Fact]
    public void SampleReports_UseOnlyValueSetCodes()
    {
        Assert.Empty(CrImgValidator.Validate(SampleReports.Level1()));
        Assert.Empty(CrImgValidator.Validate(SampleReports.Minimal()));
    }

    [Fact]
    public void CodesOutsideValueSets_AreReported()
    {
        var cr = SampleReports.Level1();
        cr.Confidentialite = new Code("X", CodeSystems.Hl7Confidentiality, "?");
        cr.Auteurs[0].Fonction = new Code("PRF", CodeSystems.Hl7ParticipationFunction, "?");
        cr.Auteurs[0].Professionnel.Profession = Code.ProfessionSavoirFaire("G15_10/XX99", "?");
        cr.Auteurs[0].Professionnel.Nom!.Prefix = "MLLE";
        cr.Auteurs[0].Professionnel.Nom!.Suffix = "DOC";
        cr.Actes[0].Modalites[0] = Code.Dcm("SCANNER", "?");
        cr.Actes[0].RegionsAnatomiques[0] = Code.Snomed("123456", "?");
        cr.PriseEnCharge!.Modalite = new Code("URG", CodeSystems.Hl7ActCode, "?");
        cr.PriseEnCharge.Lieu!.CadreExercice = new Code("SA99", CodeSystems.CadreExercice, "?");
        cr.Custodian!.SecteurActivite = new Code("CLINIQUE", CodeSystems.SecteurActivite, "?");

        var paths = Paths(cr).ToList();

        Assert.Contains("Confidentialite", paths);
        Assert.Contains("Auteurs[0].Fonction", paths);
        Assert.Contains("Auteurs[0].Professionnel.Profession", paths);
        Assert.Contains("Auteurs[0].Professionnel.Nom.Prefix", paths);
        Assert.Contains("Auteurs[0].Professionnel.Nom.Suffix", paths);
        Assert.Contains("Actes[0].Modalites[0]", paths);
        Assert.Contains("Actes[0].RegionsAnatomiques[0]", paths);
        Assert.Contains("PriseEnCharge.Modalite", paths);
        Assert.Contains("PriseEnCharge.Lieu.CadreExercice", paths);
        Assert.Contains("Custodian.SecteurActivite", paths);
        Assert.Contains(CrImgValidator.Validate(cr), i => i.Message.Contains("JDV_J01_XdsAuthorSpecialty_CISIS"));
    }

    [Fact]
    public void CodeSystem_IsPartOfTheCheck()
    {
        var cr = SampleReports.Minimal();
        cr.Actes[0].Modalites[0] = new Code("CT", CodeSystems.SnomedCt, "Tomodensitométrie");

        Assert.Contains("Actes[0].Modalites[0]", Paths(cr));
    }

    [Fact]
    public void TerminologyChecks_CanBeDisabled()
    {
        var cr = SampleReports.Minimal();
        cr.Actes[0].Modalites[0] = Code.Dcm("SCANNER", "?");

        Assert.Empty(Paths(cr, new CrImgValidationOptions { ControlerTerminologies = false }));
    }

    [Fact]
    public void ActeCode_IsCheckedWhenLoincValueSetIsProvided()
    {
        var options = new CrImgValidationOptions
        {
            ActesImagerie = JeuDeValeurs.Charger(Path.Combine(RepoPaths.Root, "jeuxDeValeurs", "jdv-code-document-imagerie-cisis.xml")),
        };
        var cr = SampleReports.Minimal();
        Assert.Empty(Paths(cr, options));

        cr.Actes[0].Code = Code.Loinc("99999-9", "Inconnu");
        Assert.Contains("Actes[0].Code", Paths(cr, options));
        Assert.DoesNotContain("Actes[0].Code", Paths(cr));
    }

    [Fact]
    public void Defaut_ReturnsIndependentInstances()
    {
        CrImgValidationOptions.Defaut.ControlerTerminologies = false;

        Assert.True(CrImgValidationOptions.Defaut.ControlerTerminologies);
    }
}
