using System.Xml;
using CdaToHtmlLib;

namespace CdaToHtmlLib.Tests;

/// <summary>
/// Tests d'intégration de <see cref="CdaTransformer"/>.
/// Chaque test charge un fichier CDA réel depuis le répertoire TestData
/// et vérifie que la transformation produit un HTML valide et non vide.
/// </summary>
public class CdaTransformerTests
{
    private static string TestDataPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", fileName);

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string ReadTestFile(string fileName) =>
        File.ReadAllText(TestDataPath(fileName));

    // -------------------------------------------------------------------------
    // TransformCdaToHtml — signature principale (CdaFo par défaut)
    // -------------------------------------------------------------------------

    [Fact]
    public void TransformCdaToHtml_WithDluCda_ReturnsNonEmptyString()
    {
        string xml = ReadTestFile("DLU-FR-SU_2025.01.xml");

        string html = CdaTransformer.TransformCdaToHtml(xml);

        Assert.False(string.IsNullOrWhiteSpace(html));
    }

    [Fact]
    public void TransformCdaToHtml_WithDluCda_ReturnsValidHtml()
    {
        string xml = ReadTestFile("DLU-FR-SU_2025.01.xml");

        string html = CdaTransformer.TransformCdaToHtml(xml);

        Assert.Contains("<html", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TransformCdaToHtml_WithDluCda_ReturnsHtmlContainingBody()
    {
        string xml = ReadTestFile("DLU-FR-SU_2025.01.xml");

        string html = CdaTransformer.TransformCdaToHtml(xml);

        Assert.Contains("<body", html, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // TransformCdaToHtml — feuille de style CrBio
    // -------------------------------------------------------------------------

    [Fact]
    public void TransformCdaToHtml_CrBioStylesheet_WithBioCda_ReturnsNonEmptyString()
    {
        string xml = ReadTestFile("BIO-CR-BIO_2024.01_TSH_1.xml");

        string html = CdaTransformer.TransformCdaToHtml(xml, CdaStylesheet.CrBio);

        Assert.False(string.IsNullOrWhiteSpace(html));
    }

    [Fact]
    public void TransformCdaToHtml_CrBioStylesheet_WithBioCda_ReturnsValidHtml()
    {
        string xml = ReadTestFile("BIO-CR-BIO_2024.01_TSH_1.xml");

        string html = CdaTransformer.TransformCdaToHtml(xml, CdaStylesheet.CrBio);

        Assert.Contains("<html", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TransformCdaToHtml_CdaFoStylesheet_WithBioCda_ReturnsValidHtml()
    {
        string xml = ReadTestFile("BIO-CR-BIO_2024.01_TSH_1.xml");

        string html = CdaTransformer.TransformCdaToHtml(xml, CdaStylesheet.CdaFo);

        Assert.Contains("<html", html, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // Cas d'erreur
    // -------------------------------------------------------------------------

    [Fact]
    public void TransformCdaToHtml_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CdaTransformer.TransformCdaToHtml(null!));
    }

    [Fact]
    public void TransformCdaToHtml_InvalidXml_ThrowsXmlException()
    {
        Assert.Throws<XmlException>(() => CdaTransformer.TransformCdaToHtml("not xml"));
    }

    // -------------------------------------------------------------------------
    // Idempotence — deux appels successifs produisent le même résultat
    // -------------------------------------------------------------------------

    [Fact]
    public void TransformCdaToHtml_CalledTwice_ProducesSameResult()
    {
        string xml = ReadTestFile("DLU-FR-SU_2025.01.xml");

        string html1 = CdaTransformer.TransformCdaToHtml(xml);
        string html2 = CdaTransformer.TransformCdaToHtml(xml);

        Assert.Equal(html1, html2);
    }
}
