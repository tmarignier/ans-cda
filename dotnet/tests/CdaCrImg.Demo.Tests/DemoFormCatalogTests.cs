using CdaCrImg.Demo.Form;
using CdaCrImg.Serialization;
using CdaCrImg.Validation;

namespace CdaCrImg.Demo.Tests;

/// <summary>
/// Vérifie que le formulaire de démonstration dit vrai : la mention obligatoire / facultatif de chaque
/// champ correspond au comportement de la librairie (<see cref="CrImgValidator"/>).
/// </summary>
public class DemoFormCatalogTests
{
    private static readonly byte[] Pdf = SampleReports.AnsPdf;

    public static TheoryData<string> Keys(Requirement requirement)
    {
        var data = new TheoryData<string>();
        foreach (var field in FormCatalog.Fields.Where(f => f.Requirement == requirement && f.Kind != FieldKind.File))
        {
            data.Add(field.Key);
        }
        return data;
    }

    public static TheoryData<string> RequiredKeys() => Keys(Requirement.Required);
    public static TheoryData<string> OptionalKeys() => Keys(Requirement.Optional);
    public static TheoryData<string> ConditionalKeys() => Keys(Requirement.RequiredIfGroup);

    [Fact]
    public void ExampleValues_ProduceCompleteAndXsdValidReport()
    {
        var (report, inputErrors) = ReportFormMapper.Map(FormCatalog.Examples(), Pdf);

        Assert.Empty(inputErrors);
        Assert.Empty(CrImgValidator.Validate(report));
        Assert.Empty(CdaXsdValidator.Validate(CrImgWriter.Write(report)));
    }

    [Theory]
    [MemberData(nameof(RequiredKeys))]
    public void RequiredField_LeftBlank_IsRejectedByLibrary(string key)
    {
        Assert.False(IsValid(Without(key)), $"« {key} » est affiché obligatoire mais le CR reste valide sans lui.");
    }

    [Theory]
    [MemberData(nameof(OptionalKeys))]
    public void OptionalField_LeftBlank_StillProducesValidReport(string key)
    {
        Assert.True(IsValid(Without(key)), $"« {key} » est affiché facultatif mais le CR est invalide sans lui.");
    }

    [Theory]
    [MemberData(nameof(ConditionalKeys))]
    public void ConditionalField_IsRequiredOnlyWhenItsGroupIsFilled(string key)
    {
        var group = FormCatalog.Fields.Single(f => f.Key == key).Group!;
        var groupKeys = FormCatalog.Fields.Where(f => f.IsInGroup(group)).Select(f => f.Key).ToArray();

        Assert.False(IsValid(Without(key)), $"« {key} » : le CR reste valide sans lui alors que « {group} » est renseigné.");
        Assert.True(IsValid(Without(groupKeys)), $"« {key} » : le CR est invalide alors que tout le groupe « {group} » est vide.");
    }

    [Fact]
    public void PdfField_IsRequiredByLibrary()
    {
        var (report, _) = ReportFormMapper.Map(FormCatalog.Examples(), pdf: null);

        Assert.Contains(CrImgValidator.Validate(report), i => i.Path == "Corps");
    }

    [Fact]
    public void EveryField_HasDescriptionExampleAndValidOptions()
    {
        Assert.All(FormCatalog.Fields, f =>
        {
            Assert.False(string.IsNullOrWhiteSpace(f.Description), f.Key);
            Assert.False(string.IsNullOrWhiteSpace(f.Example), f.Key);
            Assert.Contains(f.Section, FormCatalog.Sections);
            if (f.Kind == FieldKind.Select) Assert.Contains(f.Options!, o => o.Code == f.Example);
            if (f.Requirement == Requirement.RequiredIfGroup) Assert.NotNull(f.GroupLabel);
        });
        Assert.Equal(FormCatalog.Fields.Count, FormCatalog.Fields.Select(f => f.Key).Distinct().Count());
    }

    private static Dictionary<string, string?> Without(params string[] keys)
    {
        var values = FormCatalog.Examples();
        foreach (var key in keys) values[key] = "";
        return values;
    }

    private static bool IsValid(Dictionary<string, string?> values)
    {
        var (report, inputErrors) = ReportFormMapper.Map(values, Pdf);
        return inputErrors.Count == 0 && CrImgValidator.Validate(report).Count == 0;
    }
}
