using CdaToHtmlLib;
using Microsoft.AspNetCore.Mvc;

namespace CdaToHtml.Web.Controllers;

public class CdaController : Controller
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Page d'accueil : formulaire d'upload d'un fichier CDA.
    /// </summary>
    [HttpGet]
    public IActionResult Index() => View();

    /// <summary>
    /// Reçoit le fichier CDA uploadé, le transforme en HTML et retourne le résultat.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Transform(IFormFile? cdaFile)
    {
        if (cdaFile is null || cdaFile.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Veuillez sélectionner un fichier CDA.");
            return View("Index");
        }

        if (cdaFile.Length > MaxFileSizeBytes)
        {
            ModelState.AddModelError(string.Empty, "Le fichier ne doit pas dépasser 10 Mo.");
            return View("Index");
        }

        string xmlCda;
        using (var reader = new StreamReader(cdaFile.OpenReadStream()))
        {
            xmlCda = await reader.ReadToEndAsync();
        }

        string html = CdaTransformer.TransformCdaToHtml(xmlCda);

        return Content(html, "text/html", System.Text.Encoding.UTF8);
    }
}
