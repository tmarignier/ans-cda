using CdaCrImg.Analyse;
using CdaCrImg.Demo.Form;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CdaCrImg.Demo.Pages;

/// <summary>
/// Analyse d'un CDA existant (fichier déposé ou exemple de <c>Resources/</c>) : tableau des champs du
/// référentiel CdaCrImg avec leur valeur, leur caractère obligatoire ou facultatif et leur statut.
/// </summary>
[RequestFormLimits(MultipartBodyLengthLimit = MaxCdaBytes)]
[RequestSizeLimit(MaxCdaBytes + 1_000_000)]
public class AnalyserModel : PageModel
{
    private const int MaxCdaBytes = 20 * 1024 * 1024;

    /// <summary>Résultat de l'analyse (<c>null</c> tant qu'aucun document n'est analysé).</summary>
    public AnalyseCda? Analyse { get; private set; }

    /// <summary>Nom du document analysé (fichier déposé ou exemple).</summary>
    public string? Document { get; private set; }

    /// <summary>Exemple analysé, le cas échéant.</summary>
    public ExempleCda? Exemple { get; private set; }

    /// <summary>Message d'erreur (document refusé ou absent).</summary>
    public string? Erreur { get; private set; }

    /// <summary>Analyse l'exemple <paramref name="exemple"/> s'il est demandé.</summary>
    public IActionResult OnGet(string? exemple)
    {
        if (string.IsNullOrEmpty(exemple)) return Page();
        Exemple = ExemplesCda.Find(exemple);
        if (Exemple == null) return NotFound();
        Document = Exemple.Id + ".xml";
        Analyser(() => ExemplesCda.Ouvrir(exemple)!);
        return Page();
    }

    /// <summary>Analyse le fichier CDA déposé.</summary>
    public IActionResult OnPost(IFormFile? cda)
    {
        if (cda is not { Length: > 0 })
        {
            Erreur = "Choisissez un fichier CDA (XML) à analyser.";
            return Page();
        }
        Document = Path.GetFileName(cda.FileName);
        Analyser(cda.OpenReadStream);
        return Page();
    }

    private void Analyser(Func<Stream> ouvrir)
    {
        try
        {
            using var flux = ouvrir();
            Analyse = CdaAnalyseur.Analyser(flux);
        }
        catch (CdaAnalyseException e)
        {
            Erreur = e.Message;
        }
    }
}
