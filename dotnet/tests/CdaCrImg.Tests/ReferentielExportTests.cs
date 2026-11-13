using System.Text;
using CdaCrImg.Referentiel;

namespace CdaCrImg.Tests;

/// <summary>
/// Exporte le référentiel des champs dans <c>docs/cr-img/champs.csv</c> (UTF-8 avec BOM, séparateur « ; »,
/// ouvert tel quel par Excel). Le fichier n'est réécrit que si son contenu change : il reste à jour avec
/// <see cref="ReferentielChamps"/>. <c>tools/champs-xlsx.py</c> en produit la version Excel mise en forme.
/// </summary>
public class ReferentielExportTests
{
    public static string CsvPath => Path.Combine(RepoPaths.Root, "docs", "cr-img", "champs.csv");

    /// <summary>En-têtes des colonnes.</summary>
    public static readonly string[] Colonnes =
    {
        "Nom du champ", "Identifiant technique", "Obligatoire ?", "Description", "Référence à la documentation",
    };

    private static readonly Dictionary<string, string> Sources = new()
    {
        ["STD"] = "STD : spécifications techniques du volet IMG-CR-IMG 2024.01",
        ["SM"] = "SM : structuration minimale CI-SIS",
        ["XSD"] = "XSD : schéma CDA R2",
        ["INS"] = "INS : traits d'identité INS",
    };

    /// <summary>Paragraphe de docs/cr-img/champs-obligatoires.md qui décrit le champ.</summary>
    private static string Paragraphe(ChampCda champ) => champ.Origine == OrigineValeur.Librairie ? "§ 3" : champ.Section switch
    {
        ReferentielChamps.Sections.Document => "§ 1.1",
        ReferentielChamps.Sections.Patient => "§ 1.2",
        ReferentielChamps.Sections.Demande => "§ 1.4",
        ReferentielChamps.Sections.Acte => "§ 1.5",
        ReferentielChamps.Sections.PriseEnCharge => "§ 1.6",
        ReferentielChamps.Sections.Corps => "§ 2",
        _ => "§ 1.3",
    };

    public static IEnumerable<string[]> Lignes() => ReferentielChamps.Champs.Select(c => new[]
    {
        c.NomComplet,
        c.Chemin,
        c.Obligation switch
        {
            Obligation.Obligatoire => "Oui",
            Obligation.Facultatif => "Non",
            _ => $"Oui si « {c.Groupe!.NomComplet} » est renseigné",
        },
        c.Description
            + $" Cardinalité {c.Cardinalite}."
            + c.Origine switch
            {
                OrigineValeur.Librairie => $" Produit par la librairie{(c.ValeurParDefaut == null ? "" : " : " + c.ValeurParDefaut)}.",
                OrigineValeur.ValeurParDefaut => $" Valeur par défaut : {c.ValeurParDefaut}.",
                _ => "",
            }
            + $" Élément CDA : {c.XPathComplet}",
        $"docs/cr-img/champs-obligatoires.md {Paragraphe(c)} ; "
            + string.Join(" ; ", c.Source.Split(',').Select(s => Sources[s.Trim()])),
    });

    [Fact]
    public void GeneratesFieldListForExcel()
    {
        var csv = new StringBuilder();
        foreach (var ligne in Lignes().Prepend(Colonnes))
            csv.Append(string.Join(";", ligne.Select(Echapper))).Append('\n');
        var contenu = new UTF8Encoding(true).GetPreamble().Concat(new UTF8Encoding(false).GetBytes(csv.ToString())).ToArray();

        if (!File.Exists(CsvPath) || !File.ReadAllBytes(CsvPath).SequenceEqual(contenu))
            File.WriteAllBytes(CsvPath, contenu);

        Assert.Equal(ReferentielChamps.Champs.Count + 1, File.ReadAllLines(CsvPath).Length);
        Assert.Equal(ReferentielChamps.Champs.Count, Lignes().Select(l => l[1]).Distinct().Count());
        Assert.Equal(ReferentielChamps.Champs.Count, Lignes().Select(l => l[0]).Distinct().Count());
    }

    /// <summary>Champ CSV : entre guillemets s'il contient « ; », un guillemet ou un saut de ligne.</summary>
    private static string Echapper(string valeur) =>
        valeur.IndexOfAny(new[] { ';', '"', '\n', '\r' }) < 0 ? valeur : "\"" + valeur.Replace("\"", "\"\"") + "\"";
}
