using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using CdaCrImg.Referentiel;
using CdaCrImg.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CdaCrImg.Demo.Tests;

/// <summary>Page « Analyser un CDA » : dépôt d'un fichier ou choix d'un exemple, tableau des champs.</summary>
public class DemoAnalyserTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Page_OffersUploadAndExamples()
    {
        var html = WebUtility.HtmlDecode(await factory.CreateClient().GetStringAsync("/Analyser"));

        Assert.Contains("type=\"file\"", html);
        Assert.Contains("value=\"CR_C\"", html);
        Assert.DoesNotContain("<table", html);
    }

    [Fact]
    public async Task Exemple_IsAnalysed()
    {
        var html = WebUtility.HtmlDecode(await factory.CreateClient().GetStringAsync("/Analyser?exemple=CR_C"));

        Assert.Contains("Tous les champs obligatoires du référentiel sont présents.", html);
        var ins = Ligne(html, "Patient.Ins");
        Assert.Contains("1.2.250.1.213.1.4.10^279035121518989", ins);
        Assert.Contains("Obligatoire", ins);
        Assert.Contains("Présent", ins);
        Assert.Contains("Facultatif absent", Ligne(html, "MedecinsDemandeurs"));
        // Lignes du fichier d'origine (commentaires compris).
        Assert.Contains("ligne ", ins);
        Assert.Contains("/ClinicalDocument/authenticator", html);
        Assert.Contains("/ClinicalDocument/informant[1]", html);
        // Chaque section du référentiel présente dans l'analyse est affichée.
        Assert.All(ReferentielChamps.Sections.Toutes, s => Assert.Contains($"<h2>{s}</h2>", html));
    }

    [Fact]
    public async Task UnknownExemple_IsNotFound()
    {
        var response = await factory.CreateClient().GetAsync("/Analyser?exemple=inconnu");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadedCda_WithMissingRequiredField_ShowsAnomaly()
    {
        var cda = CrImgWriter.Write(SampleReports.Minimal());
        cda.Descendants(CdaNamespaces.Hl7 + "title").Single().Remove();

        var html = await PostAsync(Encoding.UTF8.GetBytes(cda.ToString()), "mon-cr.xml");

        Assert.Contains("mon-cr.xml", html);
        Assert.Contains("1 anomalie(s)", html);
        Assert.Contains("Obligatoire absent", Ligne(html, "Titre"));
        Assert.Contains("Aucun : tous les éléments du document correspondent à un champ du référentiel.", html);
    }

    [Theory]
    [InlineData("<html/>", "pas un CDA R2")]
    [InlineData("pas du XML", "XML mal formé")]
    public async Task InvalidUpload_ShowsError(string contenu, string message)
    {
        var html = await PostAsync(Encoding.UTF8.GetBytes(contenu), "document.xml");

        Assert.Contains("Le document n'a pas pu être analysé", html);
        Assert.Contains(message, html);
    }

    [Fact]
    public async Task MissingFile_ShowsError()
    {
        var html = await PostAsync(null, null);

        Assert.Contains("Choisissez un fichier CDA", html);
    }

    [Fact]
    public async Task GenerationForm_LinksToAnalysis()
    {
        var html = WebUtility.HtmlDecode(await factory.CreateClient().GetStringAsync("/?exemple=CR_C"));

        Assert.Contains("href=\"/Analyser\"", html);
        Assert.Contains("href=\"/Analyser?exemple=CR_C\"", html);
    }

    /// <summary>Envoie le fichier comme un navigateur (jeton antiforgery et cookie compris).</summary>
    private async Task<string> PostAsync(byte[]? fichier, string? nom)
    {
        var client = factory.CreateClient();
        var page = await client.GetStringAsync("/Analyser");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var content = new MultipartFormDataContent { { new StringContent(token), "__RequestVerificationToken" } };
        if (fichier != null)
        {
            var file = new ByteArrayContent(fichier);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
            content.Add(file, "cda", nom!);
        }
        var response = await client.PostAsync("/Analyser", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    }

    /// <summary>Ligne du tableau d'un champ (data-chemin).</summary>
    private static string Ligne(string html, string chemin)
    {
        var debut = html.IndexOf($"data-chemin=\"{chemin}\"", StringComparison.Ordinal);
        Assert.True(debut >= 0, $"Ligne {chemin} absente.");
        var fin = html.IndexOf("</tr>", debut, StringComparison.Ordinal);
        return html[debut..fin];
    }
}
