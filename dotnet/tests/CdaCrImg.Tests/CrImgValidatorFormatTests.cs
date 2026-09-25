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

    [Fact]
    public void Acte_CannotBeAfterDocument()
    {
        var cr = SampleReports.Minimal();
        cr.Actes[0].Debut = cr.DateCreation.AddMinutes(1);
        cr.Actes[0].Fin = cr.DateCreation.AddMinutes(30);

        Assert.Equal(new[] { "Actes[0].Debut", "Actes[0].Fin" }, Paths(cr));
    }

    [Fact]
    public void Acte_DateIsComparedAcrossTimeZones()
    {
        var cr = SampleReports.Minimal();
        // Même instant que la date du document, exprimé en UTC.
        cr.Actes[0].Debut = cr.DateCreation.ToUniversalTime();
        cr.Actes[0].Fin = null;

        Assert.Empty(Paths(cr));
    }

    [Fact]
    public void StudyInstanceUid_MustBeUniqueAcrossActes()
    {
        var cr = SampleReports.Level1();
        Assert.True(cr.Actes.Count >= 2);
        cr.Actes[1].StudyInstanceUid = cr.Actes[0].StudyInstanceUid;

        var issue = Assert.Single(CrImgValidator.Validate(cr));
        Assert.Equal("Actes[1].StudyInstanceUid", issue.Path);
        Assert.Contains("Actes[0]", issue.Message);
    }

    [Fact]
    public void DocumentRemplace_RequiresVersionAtLeastTwo()
    {
        var cr = SampleReports.Level1();
        cr.NumeroVersion = 1;

        Assert.Equal(new[] { "NumeroVersion" }, Paths(cr));

        cr.DocumentRemplace = null;
        Assert.Empty(Paths(cr));
    }

    [Fact]
    public void AccessionNumber_RequiresExtension()
    {
        var cr = SampleReports.Minimal();
        cr.Demandes[0].AccessionNumber = new Identifier("1.2.250.1.925.994044785528.27");

        Assert.Equal(new[] { "Demandes[0].AccessionNumber" }, Paths(cr));
    }

    [Theory]
    [InlineData(IdentifierRoots.IdNatPs, "801234567897", true)] // 8 + RPPS
    [InlineData(IdentifierRoots.IdNatPs, "8ABC", false)]
    [InlineData(IdentifierRoots.IdNatPs, "80123456789", false)] // RPPS de 10 chiffres
    [InlineData(IdentifierRoots.IdNatPs, "801234567897 ", false)]
    [InlineData(IdentifierRoots.IdNatPs, "0751234567", true)] // ADELI : non contrôlé
    [InlineData(IdentifierRoots.IdNatStruct, "1920008059", true)] // 1 + FINESS
    [InlineData(IdentifierRoots.IdNatStruct, "12A0000123", true)] // FINESS Corse-du-Sud
    [InlineData(IdentifierRoots.IdNatStruct, "192000805", false)] // FINESS de 8 caractères
    [InlineData(IdentifierRoots.IdNatStruct, "335000000000011", true)] // 3 + SIRET
    [InlineData(IdentifierRoots.IdNatStruct, "3350000000000", false)] // SIRET de 12 chiffres
    [InlineData(IdentifierRoots.IdNatStruct, "3ABCDEFGHIJKLMN", false)]
    public void IdNat_Format(string root, string extension, bool valid)
    {
        var cr = SampleReports.Level1();
        var path = root == IdentifierRoots.IdNatPs ? "SignataireLegal.Professionnel.Id" : "Custodian.Id";
        if (root == IdentifierRoots.IdNatPs) cr.SignataireLegal!.Professionnel.Id = new Identifier(root, extension);
        else cr.Custodian!.Id = new Identifier(root, extension);

        Assert.Equal(valid, !Paths(cr).Contains(path));
    }

    [Fact]
    public void IdNat_RequiresExtension()
    {
        var cr = SampleReports.Level1();
        cr.Custodian!.Id = new Identifier(IdentifierRoots.IdNatStruct);

        Assert.Contains("Custodian.Id", Paths(cr));
    }

    [Fact]
    public void IdNat_IsCheckedOnEveryIdentifier()
    {
        var cr = SampleReports.Level1();
        // Le radiologue et sa structure sont partagés par plusieurs rôles du compte rendu d'exemple.
        cr.Auteurs[0].Professionnel.Id = Identifier.FromRpps("ABC");
        cr.Auteurs[0].Professionnel.Organisation!.Id = Identifier.FromFiness("123");
        cr.PriseEnCharge!.Lieu!.Id = Identifier.FromSiret("123");

        var paths = Paths(cr).ToList();
        Assert.Contains("Auteurs[0].Professionnel.Id", paths);
        Assert.Contains("Auteurs[0].Professionnel.Organisation.Id", paths);
        Assert.Contains("PriseEnCharge.Lieu.Id", paths);
    }

    [Fact]
    public void Address_MustNotBeEmpty()
    {
        var cr = SampleReports.Level1();
        cr.Patient!.Adresses.Add(new Address { Use = "H" });
        cr.Custodian!.Adresses.Add(new Address { City = " " });
        cr.PriseEnCharge!.Lieu!.Adresse = new Address();

        var paths = Paths(cr).ToList();
        Assert.Contains("Patient.Adresses[1]", paths);
        Assert.Contains("Custodian.Adresses[1]", paths);
        Assert.Contains("PriseEnCharge.Lieu.Adresse", paths);
        Assert.DoesNotContain("Patient.Adresses[0]", paths);
    }

    [Fact]
    public void Address_WithOnlyCity_IsAccepted()
    {
        var cr = SampleReports.Minimal();
        cr.Patient!.Adresses.Add(new Address { City = "PARIS" });

        Assert.Empty(Paths(cr));
    }
}
