using System.Text;
using System.Xml;
using System.Xml.Linq;
using CdaCrImg.Serialization;

namespace CdaCrImg.Tests;

/// <summary>
/// Génère dans ExemplesCDA/ un exemple de CR d'imagerie produit par la librairie, à côté des exemples ANS
/// (qui ne sont jamais modifiés). Le fichier n'est réécrit que si son contenu change.
/// </summary>
public class ExampleFileTests
{
    /// <summary>Exemple niveau 1 (PDF) produit par CdaCrImg.</summary>
    public static string Level1ExamplePath => Path.Combine(RepoPaths.Examples, "CdaCrImg_IMG-CR-IMG_2024.01_CDA-R2-Niveau-1.xml");

    private static readonly object WriteLock = new();

    [Fact]
    public void GeneratesLevel1ExampleInExemplesCda()
    {
        EnsureLevel1Example();

        var written = XDocument.Load(Level1ExamplePath);
        Assert.Empty(CdaXsdValidator.Validate(written));
        Assert.Equal(SampleReports.AnsPdf, Convert.FromBase64String(
            written.Descendants(CdaNamespaces.Hl7 + "nonXMLBody").Single().Element(CdaNamespaces.Hl7 + "text")!.Value));
    }

    /// <summary>Écrit l'exemple s'il est absent ou obsolète (thread-safe : partagé avec SchematronTests).</summary>
    internal static void EnsureLevel1Example()
    {
        var doc = CrImgWriter.Write(SampleReports.Level1());
        // Mêmes instructions de traitement que les exemples ANS : rendu navigateur et validation Oxygen.
        doc.Root!.AddBeforeSelf(
            new XProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"../FeuilleDeStyle/CDA-FO.xsl\""),
            new XProcessingInstruction("oxygen", "SCHSchema=\"../schematrons/profils/IHE.sch\""),
            new XProcessingInstruction("oxygen", "SCHSchema=\"../schematrons/profils/structurationMinimale/ASIP-STRUCT-MIN-StrucMin.sch\""),
            new XProcessingInstruction("oxygen", "SCHSchema=\"../schematrons/profils/CI-SIS_ModelesDeContenusCDA.sch\""),
            new XComment(" Exemple généré par la librairie CdaCrImg (test ExampleFileTests) : ne pas modifier à la main. "));

        var content = Serialize(doc);
        lock (WriteLock)
        {
            if (!File.Exists(Level1ExamplePath) || File.ReadAllText(Level1ExamplePath) != content)
            {
                File.WriteAllText(Level1ExamplePath, content, new UTF8Encoding(false));
            }
        }
    }

    /// <summary>UTF-8 sans BOM, fins de ligne LF quel que soit l'OS (fichier versionné).</summary>
    private static string Serialize(XDocument doc)
    {
        var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true, NewLineChars = "\n" };
        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, settings))
        {
            doc.Save(writer);
        }
        return new UTF8Encoding(false).GetString(stream.ToArray()) + "\n";
    }
}
