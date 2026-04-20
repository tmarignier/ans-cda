using System.Xml;
using System.Xml.Xsl;

namespace CdaToHtml;

/// <summary>
/// Fournit la logique de transformation d'un fichier CDA (XML) en HTML via une feuille de style XSL.
/// </summary>
public static class CdaHtmlGenerator
{
    /// <summary>
    /// Transforme un fichier CDA XML en fichier HTML en appliquant une feuille de style XSL.
    /// </summary>
    /// <param name="cdaFilePath">Chemin vers le fichier CDA source (.xml).</param>
    /// <param name="xslFilePath">Chemin vers la feuille de style XSL (.xsl).</param>
    /// <param name="outputHtmlPath">Chemin du fichier HTML à générer.</param>
    /// <exception cref="FileNotFoundException">Si le fichier CDA ou XSL est introuvable.</exception>
    public static void GenerateHtml(string cdaFilePath, string xslFilePath, string outputHtmlPath)
    {
        if (!File.Exists(cdaFilePath))
            throw new FileNotFoundException("Fichier CDA introuvable.", cdaFilePath);

        if (!File.Exists(xslFilePath))
            throw new FileNotFoundException("Feuille de style XSL introuvable.", xslFilePath);

        // Assure que le répertoire de sortie existe
        string? outputDir = Path.GetDirectoryName(Path.GetFullPath(outputHtmlPath));
        if (!string.IsNullOrEmpty(outputDir))
            Directory.CreateDirectory(outputDir);

        var xslt = new XslCompiledTransform(enableDebug: false);

        // enableDocumentFunction : les feuilles de style ANS utilisent document('cda_l10n.xml')
        // enableScript           : certaines feuilles utilisent des extensions msxsl:script
        var xsltSettings = new XsltSettings(enableDocumentFunction: true, enableScript: true);

        // Le resolver permet de résoudre les xsl:import / xsl:include et les appels document()
        // relatifs au répertoire du XSL. Il est utilisé à la fois au chargement et à la transformation.
        var xmlResolver = new XmlUrlResolver();

        // On utilise une URI de fichier absolue pour que le resolver puisse correctement
        // résoudre les chemins relatifs référencés par document() dans la feuille de style.
        string xslUri = new Uri(Path.GetFullPath(xslFilePath)).AbsoluteUri;
        xslt.Load(xslUri, xsltSettings, xmlResolver);

        var readerSettings = new XmlReaderSettings
        {
            // Les fichiers CDA peuvent référencer une DTD HL7 non accessible localement
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null
        };

        using var cdaReader = XmlReader.Create(cdaFilePath, readerSettings);
        using var fileStream = new FileStream(outputHtmlPath, FileMode.Create, FileAccess.Write);
        using var htmlWriter = new StreamWriter(fileStream, System.Text.Encoding.UTF8);

        // On utilise l'overload XmlWriter afin de pouvoir passer le resolver,
        // nécessaire pour que les appels document() dans la feuille de style soient résolus.
        var xmlWriterSettings = new XmlWriterSettings
        {
            Encoding = System.Text.Encoding.UTF8,
            Indent = false,
            OmitXmlDeclaration = true
        };
        using var xmlWriter = XmlWriter.Create(htmlWriter, xmlWriterSettings);

        xslt.Transform(cdaReader, arguments: null, xmlWriter, xmlResolver);
    }
}
