using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CdaCrImg.Demo.Form;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace CdaCrImg.Tests.Demo;

/// <summary>Tests de bout en bout de l'application web de démonstration (formulaire → CDA XML).</summary>
public class DemoWebAppTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Form_ShowsEveryFieldWithRequirementDescriptionAndExample()
    {
        var html = WebUtility.HtmlDecode(await factory.CreateClient().GetStringAsync("/"));

        Assert.All(FormCatalog.Fields, field =>
        {
            var block = FieldBlock(html, field.Key);
            Assert.Contains(field.RequirementLabel, block);
            Assert.Contains(field.Description, block);
            Assert.Contains(field.Example, block);
        });
    }

    [Fact]
    public async Task SubmittingExamples_ReturnsCdaXml()
    {
        var response = await PostAsync(FormCatalog.Examples());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/xml", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("inline", response.Content.Headers.ContentDisposition!.DispositionType);
        var cda = XDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Empty(CdaXsdValidator.Validate(cda));
        Assert.Equal("PAT-TROIS", cda.Descendants(CdaNamespaces.Hl7 + "family").First().Value);
    }

    [Fact]
    public async Task DownloadAction_ReturnsCdaAsAttachment()
    {
        var response = await PostAsync(FormCatalog.Examples(), action: "telecharger");

        Assert.Equal("attachment", response.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Equal("cr-imagerie.xml", response.Content.Headers.ContentDisposition.FileName?.Trim('"'));
    }

    [Fact]
    public async Task UploadedPdf_IsEmbeddedInCda()
    {
        var pdf = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n% CR de test\n%%EOF\n");

        var response = await PostAsync(FormCatalog.Examples(), pdf: pdf);

        var cda = XDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(pdf, Convert.FromBase64String(cda.Descendants(CdaNamespaces.Hl7 + "nonXMLBody").Single().Value));
    }

    [Fact]
    public async Task MissingRequiredField_RedisplaysFormWithError()
    {
        var values = FormCatalog.Examples();
        values["patient.nomNaissance"] = "";

        var response = await PostAsync(values);

        Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Le compte rendu n'a pas pu être généré", html);
        Assert.Contains("Champ obligatoire.", FieldBlock(html, "patient.nomNaissance"));
        Assert.Contains("Patient.NomNaissance", html);
        // Les autres saisies sont conservées.
        Assert.Contains("value=\"DOMINIQUE MARIE-LOUISE\"", html);
    }

    [Fact]
    public async Task InvalidPdf_IsRejectedByLibrary()
    {
        var response = await PostAsync(FormCatalog.Examples(), pdf: new byte[] { 1, 2, 3, 4, 5, 6 });

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Corps.Pdf", html);
    }

    [JavaTheory]
    [Trait("Category", "Schematron")]
    [InlineData("profils/structurationMinimale/ASIP-STRUCT-MIN-StrucMin")]
    [InlineData("profils/CI-SIS_ModelesDeContenusCDA")]
    [InlineData("profils/IHE")]
    public async Task SubmittingExamples_ReturnsCdaPassingTransverseProfiles(string schematron)
    {
        var response = await PostAsync(FormCatalog.Examples());
        var file = Path.Combine(Path.GetTempPath(), $"cdacrimg-demo-{Guid.NewGuid():N}.xml");
        await File.WriteAllTextAsync(file, await response.Content.ReadAsStringAsync());
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

    /// <summary>Envoie le formulaire comme un navigateur (jeton antiforgery et cookie compris).</summary>
    private async Task<HttpResponseMessage> PostAsync(Dictionary<string, string?> values, string action = "afficher", byte[]? pdf = null)
    {
        var client = factory.CreateClient();
        var page = await client.GetStringAsync("/");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

        var content = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent(action), "action" },
        };
        foreach (var (key, value) in values)
        {
            if (FormCatalog.Fields.Single(f => f.Key == key).Kind == FieldKind.Checkbox && value != "true") continue;
            content.Add(new StringContent(value ?? ""), key);
        }
        if (pdf != null)
        {
            var file = new ByteArrayContent(pdf);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(file, "pdf", "cr.pdf");
        }
        return await client.PostAsync("/", content);
    }

    /// <summary>Extrait le bloc HTML d'un champ (div data-field).</summary>
    private static string FieldBlock(string html, string key)
    {
        var start = html.IndexOf($"data-field=\"{key}\"", StringComparison.Ordinal);
        Assert.True(start >= 0, $"Champ {key} absent du formulaire.");
        var end = html.IndexOf("data-field=\"", start + 12, StringComparison.Ordinal);
        return html[start..(end < 0 ? html.Length : end)];
    }
}
