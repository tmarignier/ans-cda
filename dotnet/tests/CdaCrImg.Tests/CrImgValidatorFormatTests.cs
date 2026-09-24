using CdaCrImg.Model;
using CdaCrImg.Model.Hl7;
using CdaCrImg.Validation;

namespace CdaCrImg.Tests;

/// <summary>Règles de format et de cohérence (types de données contrôlés par la structuration minimale).</summary>
public class CrImgValidatorFormatTests
{
    private static IEnumerable<string> Paths(CompteRenduImagerie cr) => CrImgValidator.Validate(cr).Select(i => i.Path);

    [Theory]
    [InlineData("1.2.250.1.213.1.1.1.45.2024.2.1", true)]
    [InlineData("90E1C8EC-F951-4B26-A305-A34848818DD6", true)]
    [InlineData("1.2.250.01", false)]
    [InlineData("identifiant-local", false)]
    [InlineData("1.2.3 ", false)]
    public void IdentifierRoot_MustBeUid(string root, bool valid)
    {
        var cr = SampleReports.Minimal();
        cr.Id = new Identifier(root);

        Assert.Equal(valid, !Paths(cr).Contains("Id"));
    }

    [Fact]
    public void Uid_IsCheckedOnEveryIdentifier()
    {
        var cr = SampleReports.Level1();
        cr.Patient!.AutresIdentifiants[0] = new Identifier("IPP", "1234");
        cr.Demandes[0].AccessionNumber = new Identifier("RIS", "105234751");
        cr.PriseEnCharge!.Lieu!.Id = new Identifier("FINESS", "920008059");

        Assert.Equal(
            new[] { "Patient.AutresIdentifiants[0]", "Demandes[0].AccessionNumber", "PriseEnCharge.Lieu.Id" },
            Paths(cr));
    }

    [Fact]
    public void Code_MustNotContainWhitespace()
    {
        var cr = SampleReports.Minimal();
        cr.Actes[0].Code = Code.Loinc("24727 0");

        Assert.Contains("Actes[0].Code", Paths(cr));
    }

    [Theory]
    [InlineData("tel:0144534551", true)]
    [InlineData("mailto:dr@exemple.fr", true)]
    [InlineData("0144534551", false)]
    [InlineData("tel:", false)]
    public void Telecom_MustBeUrl(string value, bool valid)
    {
        var cr = SampleReports.Minimal();
        cr.Patient!.Telecoms.Add(new Telecom(value));

        Assert.Equal(valid, !Paths(cr).Contains("Patient.Telecoms[0]"));
    }

    [Theory]
    [InlineData(IdentifierRoots.InsNirTest, "279035121518989", true)]
    [InlineData(IdentifierRoots.InsNirTest, "279035121518900", true)] // clé non contrôlée pour les INS de test
    [InlineData(IdentifierRoots.InsNir, "279035121518989", true)]
    [InlineData(IdentifierRoots.InsNir, "279035121518900", false)] // clé invalide
    [InlineData(IdentifierRoots.InsNir, "1690299350029", false)] // 13 caractères
    [InlineData(IdentifierRoots.InsNir, "1690299350029AB", false)]
    [InlineData(IdentifierRoots.InsNir, "269022A004001" + "88", true)] // Corse-du-Sud (2A → 19)
    public void InsMatricule_FormatAndKey(string root, string matricule, bool valid)
    {
        var cr = SampleReports.Minimal();
        cr.Patient!.Ins = new Identifier(root, matricule);

        Assert.Equal(valid, !Paths(cr).Contains("Patient.Ins"));
    }

    [Theory]
    [InlineData("51215", true)]
    [InlineData("2A004", true)]
    [InlineData("99350", true)]
    [InlineData("5121", false)]
    [InlineData("DOMPREMY", false)]
    public void LieuNaissanceCog_Format(string cog, bool valid)
    {
        var cr = SampleReports.Minimal();
        cr.Patient!.LieuNaissanceCog = cog;

        Assert.Equal(valid, !Paths(cr).Contains("Patient.LieuNaissanceCog"));
    }

    [Theory]
    [InlineData("fr-FR", true)]
    [InlineData("fr", true)]
    [InlineData("français", false)]
    public void Langue_Format(string langue, bool valid)
    {
        var cr = SampleReports.Minimal();
        cr.Langue = langue;

        Assert.Equal(valid, !Paths(cr).Contains("Langue"));
    }

    [Fact]
    public void Dates_MustBeConsistent()
    {
        var cr = SampleReports.Level1();
        cr.Actes[0].Fin = cr.Actes[0].Debut!.Value.AddMinutes(-1);
        cr.PriseEnCharge!.Fin = cr.PriseEnCharge.Debut!.Value.AddDays(-1);
        cr.Patient!.DateNaissance = cr.DateCreation.Date.AddDays(1);

        Assert.Equal(new[] { "Patient.DateNaissance", "Actes[0].Fin", "PriseEnCharge.Fin" }, Paths(cr));
    }
}
