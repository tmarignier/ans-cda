using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace CdaCrImg.Tests;

/// <summary>
/// Validation XSD CDA R2 (schémas du kit ANS, infrastructure/cda) en .NET.
/// Deux adaptations, en mémoire uniquement (les fichiers ANS ne sont pas modifiés) :
///  1. l'import de schema-for-xslt20.xsd de CDA_extended.xsd est omis (inutile au CDA, DTD non chargeable) ;
///  2. le xs:any ##other ajouté par l'ANS au type ED (general/datatypes-base.xsd) est retiré, car il viole
///     la contrainte UPA : Xerces (Java) le tolère, System.Xml refuse alors de compiler le schéma.
/// La référence reste tools/validate-cda.sh (validateur Java du kit).
/// </summary>
internal static class CdaXsdValidator
{
    private static readonly Lazy<XmlSchemaSet> Schemas = new(() =>
    {
        var pocd = new Uri(Path.Combine(RepoPaths.Root, "infrastructure", "cda", "POCD_MT000040_extended_pharmacy.xsd"));
        var root = $"""
            <xs:schema targetNamespace="urn:hl7-org:v3" xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:hl7-org:v3" elementFormDefault="qualified">
              <xs:include schemaLocation="{pocd.AbsoluteUri}"/>
              <xs:element name="ClinicalDocument" type="POCD_MT000040.ClinicalDocument"/>
            </xs:schema>
            """;

        var set = new XmlSchemaSet { XmlResolver = new PatchingResolver() };
        using (var reader = XmlReader.Create(new StringReader(root)))
        {
            set.Add(null, reader);
        }
        set.Compile();
        if (!set.GlobalElements.Contains(new XmlQualifiedName("ClinicalDocument", CdaNamespaces.Hl7.NamespaceName)))
        {
            throw new InvalidOperationException("Schéma CDA non compilé : ClinicalDocument absent.");
        }
        return set;
    });

    public static IReadOnlyList<string> Validate(XDocument document)
    {
        var errors = new List<string>();
        document.Validate(Schemas.Value, (_, e) => errors.Add($"{e.Severity}: {e.Message}"));
        return errors;
    }

    private sealed class PatchingResolver : XmlUrlResolver
    {
        private const string EdWildcard = """<xs:any minOccurs="0" namespace="##other" processContents="skip" />""";

        public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        {
            if (absoluteUri.IsFile && absoluteUri.LocalPath.EndsWith("datatypes-base.xsd", StringComparison.Ordinal))
            {
                var xsd = File.ReadAllText(absoluteUri.LocalPath);
                if (!xsd.Contains(EdWildcard))
                {
                    throw new InvalidOperationException("datatypes-base.xsd a changé : revoir le correctif UPA.");
                }
                return new MemoryStream(Encoding.UTF8.GetBytes(xsd.Replace(EdWildcard, string.Empty)));
            }
            return base.GetEntity(absoluteUri, role, ofObjectToReturn);
        }
    }
}
