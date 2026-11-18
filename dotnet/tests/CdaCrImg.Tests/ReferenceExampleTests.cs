using System.Reflection;
using System.Xml.Linq;

namespace CdaCrImg.Tests;

/// <summary>
/// Garde-fous sur le socle : les constantes de la librairie doivent correspondre aux artefacts
/// officiels ANS présents dans le dépôt, et le harnais de validation XSD doit fonctionner.
/// </summary>
public class ReferenceExampleTests
{
    [Fact]
    public void ReferenceExample_IsValidAgainstCdaXsd()
    {
        var doc = XDocument.Load(RepoPaths.CrImgReferenceExample);

        Assert.Empty(CdaXsdValidator.Validate(doc));
    }

    [Fact]
    public void XsdValidator_DetectsUnknownElement()
    {
        var doc = XDocument.Load(RepoPaths.CrImgReferenceExample);
        doc.Root!.Element(CdaNamespaces.Hl7 + "title")!.AddAfterSelf(new XElement(CdaNamespaces.Hl7 + "inconnu"));

        Assert.NotEmpty(CdaXsdValidator.Validate(doc));
    }

    [Fact]
    public void XsdValidator_DetectsMissingMandatoryElement()
    {
        var doc = XDocument.Load(RepoPaths.CrImgReferenceExample);
        doc.Root!.Element(CdaNamespaces.Hl7 + "id")!.Remove();

        Assert.NotEmpty(CdaXsdValidator.Validate(doc));
    }

    [Fact]
    public void Level1Example_IsValidAgainstCdaXsd()
    {
        Assert.Empty(CdaXsdValidator.Validate(XDocument.Load(RepoPaths.CrImgLevel1Example)));
    }

    [Fact]
    public void ReferenceExample_DeclaresCrImgHeaderTemplateIds()
    {
        var doc = XDocument.Load(RepoPaths.CrImgReferenceExample);
        var roots = doc.Root!.Elements(CdaNamespaces.Hl7 + "templateId")
            .Select(t => (string?)t.Attribute("root"))
            .ToList();

        Assert.Contains(TemplateIds.Document.CrImg, roots);
        Assert.Contains(TemplateIds.Document.DicomImagingReport, roots);
        Assert.Contains(TemplateIds.Document.DicomGeneralHeader, roots);
        Assert.Contains(TemplateIds.Document.DicomImagingHeader, roots);
        Assert.Equal(Codes.DocumentType, (string?)doc.Root.Element(CdaNamespaces.Hl7 + "code")!.Attribute("code"));
    }

    public static TheoryData<string, string> SpecConstants()
    {
        var data = new TheoryData<string, string>();
        foreach (var type in new[] { typeof(TemplateIds), typeof(Codes) })
        {
            foreach (var (name, value) in Constants(type))
            {
                data.Add(name, value);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(SpecConstants))]
    public void SpecConstant_IsFoundInAnsArtifacts(string name, string value)
    {
        Assert.True(AnsCorpus.Value.Contains($"'{value}'") || AnsCorpus.Value.Contains($"\"{value}\""),
            $"{name} = \"{value}\" introuvable dans les exemples/schématrons IMG-CR-IMG du kit ANS.");
    }

    private static readonly Lazy<string> AnsCorpus = new(() =>
    {
        var files = new[] { RepoPaths.CrImgReferenceExample, RepoPaths.CrImgLevel1Example,
                Path.Combine(RepoPaths.Schematrons, "CI-SIS_IMG-CR-IMG_2024.01.sch") }
            .Concat(Directory.EnumerateFiles(
                Path.Combine(RepoPaths.Schematrons, "include", "specificationsVolets", "IMG-CR-IMG_2024.01"),
                "*.sch", SearchOption.AllDirectories));
        return string.Join("\n", files.Select(File.ReadAllText));
    });

    private static IEnumerable<(string Name, string Value)> Constants(Type type)
    {
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static)
                     .Where(f => f.IsLiteral && f.FieldType == typeof(string)))
        {
            yield return ($"{type.Name}.{field.Name}", (string)field.GetRawConstantValue()!);
        }
        foreach (var nested in type.GetNestedTypes())
        {
            foreach (var c in Constants(nested))
            {
                yield return c;
            }
        }
    }
}
