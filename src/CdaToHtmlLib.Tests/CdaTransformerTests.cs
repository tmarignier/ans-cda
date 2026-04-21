using System.Xml;
using CdaToHtmlLib;

namespace CdaToHtmlLib.Tests;

/// <summary>
/// Tests d'intégration de <see cref="CdaTransformer"/>.
/// Le fichier CDA de référence est <c>CR_C.xml</c> (compte-rendu chirurgical).
/// Le résultat HTML attendu est stocké dans <c>TestData/CR_C.expected.html</c> :
/// tout changement de feuille de style sera visible dans le diff de ce fichier.
/// </summary>
public class CdaTransformerTests
{
    private static string TestDataPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", fileName);

    private static string ReadTestFile(string fileName) =>
        File.ReadAllText(TestDataPath(fileName));

    // -------------------------------------------------------------------------
    // Snapshot — la transformation doit produire exactement le HTML de référence
    // -------------------------------------------------------------------------

    [Fact]
    public void TransformCdaToHtml_WithCrCCda_MatchesSnapshot()
    {
        // Arrange
        string xml      = ReadTestFile("CR_C.xml");
        string expected = ReadTestFile("CR_C.expected.html");

        // Act
        string html = CdaTransformer.TransformCdaToHtml(xml);

        // Assert
        Assert.Equal(expected, html);
    }

    // -------------------------------------------------------------------------
    // Cas d'erreur
    // -------------------------------------------------------------------------

    [Fact]
    public void TransformCdaToHtml_NullInput_ThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => CdaTransformer.TransformCdaToHtml(null!));
    }

    [Fact]
    public void TransformCdaToHtml_InvalidXml_ThrowsXmlException()
    {
        // Arrange / Act / Assert
        Assert.Throws<XmlException>(() => CdaTransformer.TransformCdaToHtml("not xml"));
    }

    // -------------------------------------------------------------------------
    // Idempotence — deux appels successifs produisent le même résultat
    // -------------------------------------------------------------------------

    [Fact]
    public void TransformCdaToHtml_CalledTwice_ProducesSameResult()
    {
        // Arrange
        string xml = ReadTestFile("CR_C.xml");

        // Act
        string html1 = CdaTransformer.TransformCdaToHtml(xml);
        string html2 = CdaTransformer.TransformCdaToHtml(xml);

        // Assert
        Assert.Equal(html1, html2);
    }
}
