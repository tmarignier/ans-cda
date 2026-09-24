using System.Text;
using System.Xml;
using System.Xml.Linq;
using CdaCrImg.Model;
using CdaCrImg.Serialization;

namespace CdaCrImg.Tests;

/// <summary>
/// Génère dans ExemplesCDA/ des exemples de CR d'imagerie produits par la librairie, à côté des exemples ANS
/// (qui ne sont jamais modifiés). Un fichier n'est réécrit que si son contenu change.
/// </summary>
public class ExampleFileTests
{
    /// <summary>Exemple niveau 1 (PDF) complet, repris des données de l'exemple ANS.</summary>
    public static string Level1ExamplePath => Path.Combine(RepoPaths.Examples, "CdaCrImg_IMG-CR-IMG_2024.01_CDA-R2-Niveau-1.xml");

    /// <summary>Exemple niveau 1 (PDF) ne contenant que les champs obligatoires (docs/cr-img/champs-obligatoires.md).</summary>
    public static string MinimalExamplePath => Path.Combine(RepoPaths.Examples, "CdaCrImg_IMG-CR-IMG_2024.01_CDA-R2-Niveau-1_minimal.xml");

    /// <summary>Exemples générés : nom → (chemin, fabrique du CR).</summary>
    internal static readonly IReadOnlyDictionary<string, (Func<string> Path, Func<CompteRenduImagerie> Report)> Examples =
        new Dictionary<string, (Func<string>, Func<CompteRenduImagerie>)>
        {
            ["complet"] = (() => Level1ExamplePath, SampleReports.Level1),
            ["minimal"] = (() => MinimalExamplePath, SampleReports.Minimal),
        };

    private static readonly object WriteLock = new();

    [Fact]
    public void GeneratesLevel1ExampleInExemplesCda()
    {
        var written = XDocument.Load(EnsureExample("complet"));

        Assert.Empty(CdaXsdValidator.Validate(written));
        Assert.Equal(SampleReports.AnsPdf, Convert.FromBase64String(
            written.Descendants(CdaNamespaces.Hl7 + "nonXMLBody").Single().Element(CdaNamespaces.Hl7 + "text")!.Value));
    }

    [Fact]
    public void GeneratesMinimalExampleInExemplesCda()
    {
        var written = XDocument.Load(EnsureExample("minimal"));
        var v3 = CdaNamespaces.Hl7;

        Assert.Empty(CdaXsdValidator.Validate(written));
        // Aucun champ facultatif : ni adresse postale, ni télécom, ni nom de PS, ni médecin demandeur, ni CCAM.
        // Seule adresse présente : le lieu de naissance (code COG), trait INS obligatoire.
        Assert.Equal(new[] { "birthplace" }, written.Descendants(v3 + "addr").Select(a => a.Parent!.Parent!.Name.LocalName));
        Assert.Empty(written.Descendants(v3 + "telecom"));
        Assert.Empty(written.Descendants(v3 + "assignedPerson"));
        Assert.Empty(written.Root!.Elements(v3 + "participant"));
        Assert.DoesNotContain(written.Descendants(v3 + "translation"), t => (string?)t.Attribute("codeSystem") == CodeSystems.Ccam);
    }

    /// <summary>Écrit l'exemple s'il est absent ou obsolète et retourne son chemin (thread-safe : partagé avec SchematronTests).</summary>
    internal static string EnsureExample(string name)
    {
        var (path, report) = Examples[name];
        var doc = CrImgWriter.Write(report());
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
            if (!File.Exists(path()) || File.ReadAllText(path()) != content)
            {
                File.WriteAllText(path(), content, new UTF8Encoding(false));
            }
        }
        return path();
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
