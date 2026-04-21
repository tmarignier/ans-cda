using System.Net;
using System.Reflection;
using System.Xml;

namespace CdaToHtmlLib;

/// <summary>
/// Résolveur XML personnalisé qui lit les ressources embarquées dans l'assembly
/// afin de permettre aux feuilles de style XSL de résoudre les appels
/// <c>document('cda_l10n.xml')</c> et <c>document('cda_narrativeblock.xml')</c>
/// sans dépendance envers le système de fichiers.
/// </summary>
internal sealed class EmbeddedResourceXmlResolver : XmlResolver
{
    private const string EmbeddedScheme = "embedded";
    private const string EmbeddedHost = "CdaToHtmlLib";
    private const string ResourcePrefix = "CdaToHtmlLib.Resources.";

    private readonly Assembly _assembly = typeof(EmbeddedResourceXmlResolver).Assembly;

    /// <summary>
    /// URI de base à utiliser lors du chargement d'une feuille de style embarquée.
    /// </summary>
    public static readonly Uri BaseUri = new($"{EmbeddedScheme}://{EmbeddedHost}/");

    public override ICredentials? Credentials
    {
        set { /* non utilisé */ }
    }

    /// <inheritdoc />
    public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
    {
        if (string.IsNullOrEmpty(relativeUri))
            return baseUri ?? BaseUri;

        // Si l'URI relative est déjà absolue, on la retourne telle quelle.
        if (Uri.TryCreate(relativeUri, UriKind.Absolute, out Uri? absolute))
            return absolute;

        // Résolution relative : on extrait uniquement le nom de fichier afin de
        // garantir que tous les fichiers de ressources sont cherchés au même niveau.
        string fileName = Path.GetFileName(relativeUri);
        return new Uri($"{EmbeddedScheme}://{EmbeddedHost}/{fileName}");
    }

    /// <inheritdoc />
    public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
    {
        if (absoluteUri.Scheme.Equals(EmbeddedScheme, StringComparison.OrdinalIgnoreCase))
        {
            // Le dernier segment de l'URI contient le nom du fichier.
            string fileName = absoluteUri.Segments[^1];
            string resourceName = ResourcePrefix + fileName;

            Stream? stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new XmlException(
                    $"Ressource embarquée introuvable : « {resourceName} ». " +
                    $"Vérifiez que le fichier est bien inclus avec l'attribut LogicalName correct.");

            return stream;
        }

        // Pour les URI non embarquées (ex. données absolues externes) on délègue
        // au résolveur standard. Cela ne devrait pas se produire en utilisation normale.
        return new XmlUrlResolver().GetEntity(absoluteUri, role, ofObjectToReturn);
    }
}
