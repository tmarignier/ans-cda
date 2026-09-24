using CdaCrImg.Model;
using CdaCrImg.Model.Hl7;
using CdaCrImg.Serialization;
using CdaCrImg.Validation;

namespace CdaCrImg.Tests;

public class CrImgValidatorTests
{
    [Fact]
    public void SampleReport_IsComplete()
    {
        Assert.Empty(CrImgValidator.Validate(SampleReports.Level1()));
    }

    [Fact]
    public void EmptyReport_ListsMandatoryData()
    {
        var paths = CrImgValidator.Validate(new CompteRenduImagerie()).Select(i => i.Path).ToList();

        Assert.Contains("Id", paths);
        Assert.Contains("Patient", paths);
        Assert.Contains("Auteurs", paths);
        Assert.Contains("Custodian", paths);
        Assert.Contains("SignataireLegal", paths);
        Assert.Contains("Demandes", paths);
        Assert.Contains("Actes", paths);
        Assert.Contains("PriseEnCharge", paths);
        Assert.Contains("Corps", paths);
    }

    [Fact]
    public void Writer_RefusesIncompleteReport()
    {
        var cr = SampleReports.Level1();
        cr.Actes[0].Modalites.Clear();

        var ex = Assert.Throws<CrImgValidationException>(() => CrImgWriter.Write(cr));
        Assert.Contains(ex.Issues, i => i.Path == "Actes[0].Modalites");
    }

    [Fact]
    public void Ins_RequiresInsTraits()
    {
        var cr = SampleReports.Level1();
        cr.Patient!.LieuNaissanceCog = null;
        cr.Patient.DateNaissance = null;

        var paths = CrImgValidator.Validate(cr).Select(i => i.Path).ToList();

        Assert.Equal(new[] { "Patient.DateNaissance", "Patient.LieuNaissanceCog" }, paths);
    }

    [Fact]
    public void Ins_IsMandatory()
    {
        var cr = SampleReports.Level1();
        cr.Patient!.Ins = null;

        Assert.Equal(new[] { "Patient.Ins" }, CrImgValidator.Validate(cr).Select(i => i.Path));
    }

    [Fact]
    public void Sexe_MustBeKnown()
    {
        var cr = SampleReports.Level1();
        cr.Patient!.Sexe = Sexe.Inconnu;

        Assert.Equal(new[] { "Patient.Sexe" }, CrImgValidator.Validate(cr).Select(i => i.Path));
    }

    [Fact]
    public void MinimalReport_IsComplete()
    {
        Assert.Empty(CrImgValidator.Validate(SampleReports.Minimal()));
    }

    [Fact]
    public void Executant_RequiresSecteurActivite()
    {
        var cr = SampleReports.Minimal();
        cr.Actes[0].Executant!.Organisation!.SecteurActivite = null;

        Assert.Contains(CrImgValidator.Validate(cr), i => i.Path == "Actes[0].Executant.Organisation.SecteurActivite");
    }

    [Fact]
    public void Auteur_RequiresOrganisation()
    {
        var cr = SampleReports.Level1();
        cr.Auteurs[0] = new Auteur(new Professionnel { Id = Identifier.FromRpps("1"), Profession = cr.Auteurs[0].Professionnel.Profession }, cr.DateCreation);

        Assert.Contains(CrImgValidator.Validate(cr), i => i.Path == "Auteurs[0].Professionnel.Organisation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.2.3.abc")]
    [InlineData("1.02.3")]
    public void StudyInstanceUid_MustBeDicomUid(string uid)
    {
        var cr = SampleReports.Level1();
        cr.Actes[0].StudyInstanceUid = uid;

        Assert.Contains(CrImgValidator.Validate(cr), i => i.Path == "Actes[0].StudyInstanceUid");
    }

    [Fact]
    public void Acte_RequiresLoincCode()
    {
        var cr = SampleReports.Level1();
        cr.Actes[0].Code = Code.Ccam("ACQH004");

        Assert.Contains(CrImgValidator.Validate(cr), i => i.Path == "Actes[0].Code");
    }

    [Fact]
    public void Executant_RequiresOrganisationId()
    {
        var cr = SampleReports.Level1();
        cr.Actes[1].Executant = new Professionnel { Id = Identifier.FromRpps("1"), Organisation = new Organisation { Nom = "X" } };

        Assert.Contains(CrImgValidator.Validate(cr), i => i.Path == "Actes[1].Executant.Organisation.Id");
    }

    [Fact]
    public void AccessionNumber_IsMandatory()
    {
        var cr = SampleReports.Level1();
        cr.Demandes[0].AccessionNumber = Identifier.Null();

        Assert.Contains(CrImgValidator.Validate(cr), i => i.Path == "Demandes[0].AccessionNumber");
    }

    [Fact]
    public void PdfBody_MustBePdf()
    {
        var cr = SampleReports.Level1();
        cr.Corps = new CorpsPdf(new byte[] { 1, 2, 3, 4, 5, 6 });

        Assert.Contains(CrImgValidator.Validate(cr), i => i.Path == "Corps.Pdf");
    }
}
