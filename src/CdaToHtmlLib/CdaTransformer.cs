using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Xsl;

namespace CdaToHtmlLib;

/// <summary>
/// Feuille de style XSL disponible dans la librairie.
/// </summary>
public enum CdaStylesheet
{
    /// <summary>
    /// Feuille de style générique ANS pour les documents CDA (CDA-FO.xsl).
    /// Utilisée par défaut.
    /// </summary>
    CdaFo
}

/// <summary>
/// Point d'entrée de la librairie de transformation CDA → HTML.
/// Toutes les feuilles de style XSL ANS sont embarquées dans l'assembly ;
/// aucune dépendance externe n'est requise.
/// </summary>
public static class CdaTransformer
{
    // Correspondance entre l'énumération et le nom de la ressource embarquée.
    private static readonly Dictionary<CdaStylesheet, string> StylesheetFileNames =
        new()
        {
            [CdaStylesheet.CdaFo]  = "CDA-FO.xsl"
        };

    // Instances précompilées pour éviter de recharger les XSL à chaque appel.
    private static readonly Dictionary<CdaStylesheet, Lazy<XslCompiledTransform>> CompiledTransforms =
        new()
        {
            [CdaStylesheet.CdaFo]  = new Lazy<XslCompiledTransform>(() => LoadXslt(CdaStylesheet.CdaFo))
        };

    // Contenu du script datamatrix.min.js, chargé une seule fois depuis la ressource embarquée.
    private static readonly Lazy<string> DatamatrixScript = new(LoadDatamatrixScript);

    // Balise externe générée par la feuille XSL — remplacée lors du post-traitement.
    private const string ExternalScriptTag =
        @"<script src=""../FeuilleDeStyle/JS/datamatrix.min.js"" type=""text/javascript"" />";

    /// <summary>
    /// Transforme un document CDA (XML HL7 CDA R2) en HTML.
    /// La feuille de style XSL utilisée est <see cref="CdaStylesheet.CdaFo"/> (CDA-FO.xsl).
    /// </summary>
    /// <param name="xmlCda">Contenu XML du document CDA source.</param>
    /// <returns>Chaîne HTML résultant de la transformation.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="xmlCda"/> est null.</exception>
    /// <exception cref="XmlException">Si le XML source ou la feuille de style est invalide.</exception>
    public static string TransformCdaToHtml(string xmlCda)
        => TransformCdaToHtml(xmlCda, CdaStylesheet.CdaFo);

    /// <summary>
    /// Transforme un document CDA (XML HL7 CDA R2) en HTML en utilisant la feuille de style spécifiée.
    /// </summary>
    /// <param name="xmlCda">Contenu XML du document CDA source.</param>
    /// <param name="stylesheet">Feuille de style XSL à appliquer.</param>
    /// <returns>Chaîne HTML résultant de la transformation.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="xmlCda"/> est null.</exception>
    /// <exception cref="XmlException">Si le XML source ou la feuille de style est invalide.</exception>
    public static string TransformCdaToHtml(string xmlCda, CdaStylesheet stylesheet)
    {
        ArgumentNullException.ThrowIfNull(xmlCda);

        XslCompiledTransform xslt = CompiledTransforms[stylesheet].Value;
        var resolver = new EmbeddedResourceXmlResolver();

        var readerSettings = new XmlReaderSettings
        {
            // Les fichiers CDA peuvent référencer une DTD HL7 non accessible localement.
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null
        };

        using var cdaReader = XmlReader.Create(new StringReader(xmlCda), readerSettings);
        using var output = new StringWriter();
        var xmlWriterSettings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = false,
            OmitXmlDeclaration = true
        };
        using var xmlWriter = XmlWriter.Create(output, xmlWriterSettings);

        // Le resolver est passé en dernier argument pour résoudre les appels
        // document() (cda_l10n.xml, cda_narrativeblock.xml) depuis les ressources embarquées.
        xslt.Transform(cdaReader, arguments: null, xmlWriter, resolver);

        // Remplace la référence externe au script datamatrix par le contenu JS embarqué,
        // ce qui rend le HTML autonome (aucune dépendance externe requise).
        return InjectDatamatrixScript(output.ToString());
    }

    // -------------------------------------------------------------------------
    // Méthodes privées
    // -------------------------------------------------------------------------

    private static XslCompiledTransform LoadXslt(CdaStylesheet stylesheet)
    {
        string fileName = StylesheetFileNames[stylesheet];
        var xslt = new XslCompiledTransform(enableDebug: false);

        // enableDocumentFunction : les feuilles de style ANS utilisent document('cda_l10n.xml')
        // enableScript           : certaines feuilles utilisent des extensions msxsl:script
        var settings = new XsltSettings(enableDocumentFunction: true, enableScript: true);
        var resolver = new EmbeddedResourceXmlResolver();

        Uri xslUri = new(EmbeddedResourceXmlResolver.BaseUri, fileName);
        xslt.Load(xslUri.AbsoluteUri, settings, resolver);

        return xslt;
    }

    private static string LoadDatamatrixScript()
    {
        Assembly assembly = typeof(CdaTransformer).Assembly;
        using Stream stream = assembly.GetManifestResourceStream("CdaToHtmlLib.Resources.datamatrix.min.js")
            ?? throw new InvalidOperationException(
                "Ressource embarquée introuvable : CdaToHtmlLib.Resources.datamatrix.min.js");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string InjectDatamatrixScript(string html)
    {
        if (!html.Contains(ExternalScriptTag, StringComparison.Ordinal))
            return html;

        string inlineScript = $"<script type=\"text/javascript\">{DatamatrixScript.Value}</script>";
        return html.Replace(ExternalScriptTag, inlineScript, StringComparison.Ordinal);
    }
}
