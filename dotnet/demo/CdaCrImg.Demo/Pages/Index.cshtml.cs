using System.Text;
using CdaCrImg.Demo.Form;
using CdaCrImg.Serialization;
using CdaCrImg.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CdaCrImg.Demo.Pages;

/// <summary>Formulaire de démonstration : saisie des champs, puis renvoi du CDA XML produit par CdaCrImg.</summary>
[RequestFormLimits(MultipartBodyLengthLimit = MaxPdfBytes)]
[RequestSizeLimit(MaxPdfBytes + 1_000_000)]
public class IndexModel(IWebHostEnvironment environment) : PageModel
{
    private const int MaxPdfBytes = 10 * 1024 * 1024;

    /// <summary>Valeurs affichées (exemples à l'ouverture, saisie de l'utilisateur ensuite).</summary>
    public Dictionary<string, string?> Values { get; private set; } = FormCatalog.Examples();

    /// <summary>Exemple de CDA ayant pré-rempli le formulaire (<c>null</c> : valeurs d'exemple du catalogue).</summary>
    public ExempleCda? Exemple { get; private set; }

    /// <summary>Erreurs par champ (clé du champ → message).</summary>
    public Dictionary<string, List<string>> FieldErrors { get; } = new();

    /// <summary>Non-conformités renvoyées par la librairie (<see cref="CrImgValidator"/>).</summary>
    public IReadOnlyList<ValidationIssue> Issues { get; private set; } = Array.Empty<ValidationIssue>();

    /// <summary>Affiche le formulaire, pré-rempli avec l'exemple <paramref name="exemple"/> s'il est demandé.</summary>
    public IActionResult OnGet(string? exemple)
    {
        if (string.IsNullOrEmpty(exemple)) return Page();
        Exemple = ExemplesCda.Find(exemple);
        if (Exemple == null) return NotFound();
        Values = Exemple.FormValues();
        return Page();
    }

    /// <summary>PDF encapsulé dans un exemple de CDA.</summary>
    public IActionResult OnGetPdf(string? exemple) =>
        ExemplesCda.Find(exemple)?.Pdf is { } pdf ? File(pdf, "application/pdf") : NotFound();

    public async Task<IActionResult> OnPostAsync(IFormFile? pdf, string? action, string? exemple)
    {
        Values = FormCatalog.Fields
            .Where(f => f.Kind != FieldKind.File)
            .ToDictionary(f => f.Key, f => (string?)Request.Form[f.Key].ToString());
        Exemple = ExemplesCda.Find(exemple);

        CheckRequiredFields();

        // Sans fichier transmis : PDF de l'exemple sélectionné, sinon PDF d'exemple par défaut.
        var pdfBytes = pdf is { Length: > 0 } ? await ReadAsync(pdf)
            : Exemple?.Pdf ?? await System.IO.File.ReadAllBytesAsync(ExamplePdfPath);
        var (report, inputErrors) = ReportFormMapper.Map(Values, pdfBytes);
        foreach (var (key, message) in inputErrors) AddError(key, message);

        Issues = CrImgValidator.Validate(report);
        foreach (var issue in Issues)
        {
            foreach (var field in FormCatalog.Fields.Where(f => Matches(issue.Path, f.ModelPath)))
                AddError(field.Key, issue.Message);
        }

        if (FieldErrors.Count > 0 || Issues.Count > 0) return Page();

        var xml = CrImgWriter.WriteToString(report);
        var disposition = action == "telecharger" ? "attachment" : "inline";
        Response.Headers.ContentDisposition = $"{disposition}; filename=\"cr-imagerie.xml\"";
        return Content(xml, "application/xml; charset=utf-8", Encoding.UTF8);
    }

    /// <summary>Chemin du PDF d'exemple utilisé lorsqu'aucun fichier n'est transmis.</summary>
    private string ExamplePdfPath => Path.Combine(environment.WebRootPath, "exemple-cr.pdf");

    /// <summary>Obligations du formulaire : champs obligatoires, et champs obligatoires d'un groupe renseigné.</summary>
    private void CheckRequiredFields()
    {
        bool Filled(FieldDefinition f) => !string.IsNullOrWhiteSpace(Values.GetValueOrDefault(f.Key)) && f.Kind != FieldKind.Checkbox;

        foreach (var field in FormCatalog.Fields.Where(f => f.Kind != FieldKind.File && !Filled(f)))
        {
            if (field.Requirement == Requirement.Required)
                AddError(field.Key, "Champ obligatoire.");
            else if (field.Requirement == Requirement.RequiredIfGroup
                     && FormCatalog.Fields.Any(other => other.IsInGroup(field.Group!) && Filled(other)))
                AddError(field.Key, $"Champ obligatoire lorsque « {field.GroupLabel} » est renseigné.");
        }
    }

    private static bool Matches(string issuePath, string modelPath) =>
        issuePath == modelPath
        || issuePath.StartsWith(modelPath + ".", StringComparison.Ordinal)
        || issuePath.StartsWith(modelPath + "[", StringComparison.Ordinal);

    private void AddError(string key, string message)
    {
        if (!FieldErrors.TryGetValue(key, out var messages)) FieldErrors[key] = messages = new List<string>();
        if (!messages.Contains(message)) messages.Add(message);
    }

    private static async Task<byte[]> ReadAsync(IFormFile file)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        return stream.ToArray();
    }
}
