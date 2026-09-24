using CdaCrImg.Serialization;
using Xunit.Abstractions;

namespace CdaCrImg.Tests;

/// <summary>
/// Validation par l'outillage officiel du kit ANS (XSD Java + schématrons), via <see cref="AnsJavaValidator"/>.
/// Ignorés si Java est absent ; exclure explicitement avec : dotnet test --filter Category!=Schematron
/// </summary>
[Trait("Category", "Schematron")]
public class SchematronTests(ITestOutputHelper output)
{
    [JavaTheory]
    [InlineData("profils/structurationMinimale/ASIP-STRUCT-MIN-StrucMin")]
    [InlineData("profils/CI-SIS_ModelesDeContenusCDA")]
    [InlineData("profils/IHE")]
    public void Level1Document_PassesTransverseProfiles(string schematron)
    {
        var file = Path.Combine(Path.GetTempPath(), $"cdacrimg-level1-{Guid.NewGuid():N}.xml");
        File.WriteAllText(file, CrImgWriter.WriteToString(SampleReports.Level1()));
        try
        {
            var errors = AnsJavaValidator.ValidateXsd(file).Concat(AnsJavaValidator.ValidateSchematron(file, schematron)).ToList();
            errors.ForEach(output.WriteLine);
            Assert.Empty(errors);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [JavaTheory]
    [InlineData("profils/structurationMinimale/ASIP-STRUCT-MIN-StrucMin")]
    [InlineData("profils/CI-SIS_ModelesDeContenusCDA")]
    [InlineData("profils/IHE")]
    public void GeneratedExampleFile_PassesTransverseProfiles(string schematron)
    {
        ExampleFileTests.EnsureLevel1Example();

        var errors = AnsJavaValidator.ValidateXsd(ExampleFileTests.Level1ExamplePath)
            .Concat(AnsJavaValidator.ValidateSchematron(ExampleFileTests.Level1ExamplePath, schematron)).ToList();
        errors.ForEach(output.WriteLine);
        Assert.Empty(errors);
    }

    [JavaTheory]
    [InlineData("profils/structurationMinimale/ASIP-STRUCT-MIN-StrucMin")]
    public void Harness_ReportsFailedAsserts(string schematron)
    {
        // Garde-fou : un document non conforme doit bien produire des failed-assert.
        var doc = CrImgWriter.Write(SampleReports.Level1());
        doc.Root!.Element(CdaNamespaces.Hl7 + "code")!.Remove();
        var file = Path.Combine(Path.GetTempPath(), $"cdacrimg-invalid-{Guid.NewGuid():N}.xml");
        doc.Save(file);
        try
        {
            Assert.NotEmpty(AnsJavaValidator.ValidateXsd(file));
            Assert.NotEmpty(AnsJavaValidator.ValidateSchematron(file, schematron));
        }
        finally
        {
            File.Delete(file);
        }
    }
}
