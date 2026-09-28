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

    public static TheoryData<string, string> ExamplesAndProfiles()
    {
        var data = new TheoryData<string, string>();
        foreach (var example in ExampleFileTests.Examples.Keys)
        {
            foreach (var profile in new[] { "profils/structurationMinimale/ASIP-STRUCT-MIN-StrucMin", "profils/CI-SIS_ModelesDeContenusCDA", "profils/IHE" })
            {
                data.Add(example, profile);
            }
        }
        return data;
    }

    [JavaTheory]
    [MemberData(nameof(ExamplesAndProfiles))]
    public void GeneratedExampleFile_PassesTransverseProfiles(string example, string schematron)
    {
        var path = ExampleFileTests.EnsureExample(example);

        var errors = AnsJavaValidator.ValidateXsd(path).Concat(AnsJavaValidator.ValidateSchematron(path, schematron)).ToList();
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
