using System.Net;
using System.Xml.Linq;
using CdaCrImg.Demo.Form;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace CdaCrImg.Demo.Tests;

/// <summary>Exemples de CDA proposés pour pré-remplir le formulaire de démonstration (Resources/*.xml).</summary>
public class DemoExemplesTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    : IClassFixture<WebApplicationFactory<Program>>
{
    public static TheoryData<string> Exemples() => new(ExemplesCda.All.Select(e => e.Id));

    public static TheoryData<string, string> ExemplesEtProfils()
    {
        var data = new TheoryData<string, string>();
        foreach (var exemple in ExemplesCda.All)
        {
            data.Add(exemple.Id, "profils/structurationMinimale/ASIP-STRUCT-MIN-StrucMin");
            data.Add(exemple.Id, "profils/CI-SIS_ModelesDeContenusCDA");
            data.Add(exemple.Id, "profils/IHE");
        }
        return data;
    }

    [Fact]
    public void Exemples_ContainCrC()
    {
        Assert.Contains(ExemplesCda.All, e => e.Id == "CR_C");
    }

    [Theory]
    [MemberData(nameof(Exemples))]
    public void Exemple_FillsOnlyKnownFieldsWithSelectableValues(string id)
    {
        var exemple = ExemplesCda.Find(id)!;

        Assert.All(exemple.Values, pair =>
        {
            var field = FormCatalog.Fields.SingleOrDefault(f => f.Key == pair.Key);
            Assert.True(field != null, $"Champ inconnu : {pair.Key}");
            if (field.Options != null)
                Assert.True(field.Options.Any(o => o.Code == pair.Value), $"{pair.Key} : valeur {pair.Value} absente de la liste.");
        });
        Assert.NotNull(exemple.Pdf);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(exemple.Pdf!, 0, 5));
    }

    [Fact]
    public void CrC_IsReadFromCda()
    {
        var values = ExemplesCda.Find("CR_C")!.Values;

        Assert.Equal("279035121518989", values["patient.insMatricule"]);
        Assert.Equal("IPP101", values["patient.ippValeur"]);
        Assert.Equal("DOMINIQUE", values["patient.prenomsNaissance"]);
        Assert.Equal("1979-03-28", values["patient.dateNaissance"]);
        Assert.Equal("01234560801", values["auteur.rpps"]);
        Assert.Equal("750803447", values["auteur.orgFiness"]);
        Assert.Equal("2021-01-08T11:17", values["document.dateCreation"]);
        Assert.Equal("2021-01-08T10:25", values["pec.debut"]);
        Assert.Equal("ACN103", values["demande.accessionValeur"]);
        Assert.Equal("OPN103", values["demande.numeroValeur"]);
        Assert.Equal("1.2.250.1.213.4.5.2.1.103", values["acte.studyUid"]);
        Assert.Equal("ACQH004", values["acte.ccamCode"]);
        Assert.Equal("CT", values["acte.modalite"]);
        Assert.Equal("774007", values["acte.region"]);
        Assert.Equal("SA08", values["pec.cadreExercice"]);
        // Médecin traitant (participant INF) : ce n'est pas un médecin demandeur.
        Assert.DoesNotContain(values.Keys, k => k.StartsWith("demandeur.", StringComparison.Ordinal));
        Assert.DoesNotContain("acte.depistage", values.Keys);
    }

    [Fact]
    public async Task Form_ListsExemplesAndPrefillsSelectedOne()
    {
        var client = factory.CreateClient();

        var home = WebUtility.HtmlDecode(await client.GetStringAsync("/"));
        Assert.Contains("value=\"CR_C\"", home);

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/?exemple=CR_C"));
        Assert.Contains("value=\"IPP101\"", DemoWebAppTests.FieldBlock(html, "patient.ippValeur"));
        Assert.Contains("name=\"exemple\" value=\"CR_C\"", html);
        Assert.Contains("href=\"/?exemple=CR_C&handler=Pdf\"", html);
    }

    [Fact]
    public async Task UnknownExemple_IsNotFound()
    {
        var response = await factory.CreateClient().GetAsync("/?exemple=inconnu");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ExemplePdf_IsServed()
    {
        var response = await factory.CreateClient().GetAsync("/?handler=Pdf&exemple=CR_C");

        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(ExemplesCda.Find("CR_C")!.Pdf, await response.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [MemberData(nameof(Exemples))]
    public async Task SubmittingExemple_ReturnsValidCdaWithExemplePdf(string id)
    {
        var exemple = ExemplesCda.Find(id)!;

        var response = await DemoWebAppTests.PostAsync(factory, exemple.FormValues(), exemple: id);

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.Content.Headers.ContentType!.MediaType == "application/xml", body);
        var cda = XDocument.Parse(body);
        Assert.Empty(CdaXsdValidator.Validate(cda));
        Assert.Equal(exemple.Pdf, Convert.FromBase64String(cda.Descendants(CdaNamespaces.Hl7 + "nonXMLBody").Single().Value));
    }

    [JavaTheory]
    [Trait("Category", "Schematron")]
    [MemberData(nameof(ExemplesEtProfils))]
    public async Task SubmittingExemple_ReturnsCdaPassingTransverseProfiles(string id, string schematron)
    {
        var exemple = ExemplesCda.Find(id)!;
        var response = await DemoWebAppTests.PostAsync(factory, exemple.FormValues(), exemple: id);
        var file = Path.Combine(Path.GetTempPath(), $"cdacrimg-demo-{id}-{Guid.NewGuid():N}.xml");
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
}
