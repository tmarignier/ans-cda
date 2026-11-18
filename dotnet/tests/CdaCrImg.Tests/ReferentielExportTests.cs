using System.IO.Compression;
using System.Xml.Linq;
using CdaCrImg.Referentiel;

namespace CdaCrImg.Tests;

/// <summary>
/// Exporte le référentiel des champs dans le classeur Excel <c>docs/cr-img/champs.xlsx</c> (feuilles
/// « Champs » et « Légende »). Le fichier n'est réécrit que si son contenu change : il reste à jour avec
/// <see cref="ReferentielChamps"/>.
/// </summary>
public class ReferentielExportTests
{
    public static string XlsxPath => Path.Combine(RepoPaths.Root, "docs", "cr-img", "champs.xlsx");

    /// <summary>En-têtes des colonnes de la feuille « Champs ».</summary>
    public static readonly string[] Colonnes =
    {
        "Nom du champ", "Identifiant technique", "Obligatoire ?", "Description", "Référence à la documentation",
        "Source de la donnée", "Doit venir du RIS ?",
    };

    private static readonly Dictionary<string, string> Sources = new()
    {
        ["STD"] = "STD : spécifications techniques du volet IMG-CR-IMG 2024.01",
        ["SM"] = "SM : structuration minimale CI-SIS",
        ["XSD"] = "XSD : schéma CDA R2",
        ["INS"] = "INS : traits d'identité INS",
    };

    private static readonly (string, string)[] Legende =
    {
        ("Nom du champ", "Section du compte rendu, puis libellés des champs qui le contiennent."),
        ("Identifiant technique", "Chemin de la propriété dans la librairie CdaCrImg ([] = liste), identique au chemin des erreurs de CrImgValidator (sans les index)."),
        ("Obligatoire ?", "Oui / Non, ou « Oui si … est renseigné » : obligatoire dès que le groupe facultatif qui le contient est présent."),
        ("Description", "Description métier, cardinalité dans le CDA, valeur fixe ou par défaut produite par la librairie, et élément CDA (XPath ; préfixe cda = urn:hl7-org:v3)."),
        ("Référence à la documentation", "Paragraphe de docs/cr-img/champs-obligatoires.md et sources de la règle."),
        ("Source de la donnée", "Acteur ou système qui fournit la donnée, d'après les rôles décrits par l'ANS (SFD 3.2.2 et 3.2.3, STD 3.2), téléradiologie comprise : l'examen est réalisé dans la structure d'imagerie qui accueille le patient et interprété à distance par le téléradiologue. « Librairie CdaCrImg » : valeur fixe produite par la librairie."),
        ("Doit venir du RIS ?", "Oui : donnée fournie par le RIS de la structure d'imagerie qui accueille le patient (en téléradiologie, par les flux 1 et 4 du volet Téléradiologie ou directement par le RIS). Non : fournie par le logiciel du téléradiologue, l'annuaire santé, le document de demande ou la librairie. En partie : oui pour le médecin responsable de la structure d'accueil (ORC-12 du flux 1), non pour le téléradiologue ou pour la profession (annuaire)."),
        ("STD", "Volet CI-SIS IMG-CR-IMG 2024.01, spécifications techniques (docs/cr-img/ans/CI-SIS_VOLET_CONTENUS_IMG-CR-IMG_2024.01_STD_CDA_20251212.pdf)."),
        ("Volet TLR", "Volet Téléradiologie du CI-SIS, guide HL7 v2 0.2.0 (https://interop.esante.gouv.fr/ig/hl7v2/teleradiologie/) : flux 1 demande (ORM^O01), flux 3 réponse (ORU^R01), flux 4 complément post-examen (OMI^O23) ; analyse dans docs/cr-img/teleradiologie.md."),
        ("SFD", "Volet CI-SIS IMG-CR-IMG 2024.01, spécifications fonctionnelles (docs/cr-img/ans/CI-SIS_VOLET_CONTENUS_IMG-CR-IMG_2024.01_SFD_20251212.pdf)."),
        ("SM", "Structuration minimale des documents de santé CI-SIS (schematrons/profils/structurationMinimale)."),
        ("XSD", "Schéma CDA R2 (infrastructure/cda/CDA_extended.xsd)."),
        ("INS", "Identité nationale de santé : matricule et traits d'identité."),
        ("Source des données du classeur", "Référentiel des champs de la librairie CdaCrImg (dotnet/src/CdaCrImg/Referentiel/ReferentielChamps.cs), vérifié par les tests contre CrImgValidator, CrImgWriter et l'exemple ANS niveau 1 ; classeur régénéré par le test ReferentielExportTests."),
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

    private static (string, StyleCellule)[] Ligne(ChampCda c)
    {
        var (obligatoire, style) = c.Obligation switch
        {
            Obligation.Obligatoire => ("Oui", StyleCellule.Oui),
            Obligation.Facultatif => ("Non", StyleCellule.Non),
            _ => ($"Oui si « {c.Groupe!.NomComplet} » est renseigné", StyleCellule.Conditionnel),
        };
        var description = c.Description
            + $" Cardinalité {c.Cardinalite}."
            + c.Origine switch
            {
                OrigineValeur.Librairie => $" Produit par la librairie{(c.ValeurParDefaut == null ? "" : " : " + c.ValeurParDefaut)}.",
                OrigineValeur.ValeurParDefaut => $" Valeur par défaut : {c.ValeurParDefaut}.",
                _ => "",
            }
            + $" Élément CDA : {c.XPathComplet}";
        var reference = $"docs/cr-img/champs-obligatoires.md {Paragraphe(c)} ; "
            + string.Join(" ; ", c.Source.Split(',').Select(s => Sources[s.Trim()]));
        return new[]
        {
            (c.NomComplet, c.Enfants.Count > 0 ? StyleCellule.TexteGras : StyleCellule.Texte),
            (c.Chemin, StyleCellule.Code),
            (obligatoire, style),
            (description, StyleCellule.Texte),
            (reference, StyleCellule.Texte),
            (c.SourceDonnee, StyleCellule.Texte),
            (c.FourniParRis, c.FourniParRis switch
            {
                "Oui" => StyleCellule.Oui,
                "Non" => StyleCellule.Non,
                _ => StyleCellule.Conditionnel,
            }),
        };
    }

    internal static IReadOnlyList<FeuilleXlsx> Feuilles() => new[]
    {
        new FeuilleXlsx("Champs", new[] { 50.0, 44, 22, 80, 55, 60, 18 },
            ReferentielChamps.Champs.Select(Ligne).Prepend(Colonnes.Select(c => (c, StyleCellule.Entete)).ToArray()).ToList()),
        new FeuilleXlsx("Légende", new[] { 30.0, 120 },
            Legende.Select(l => new[] { (l.Item1, StyleCellule.TexteGras), (l.Item2, StyleCellule.Texte) })
                .Prepend(new[] { ("Colonne / abréviation", StyleCellule.Entete), ("Signification", StyleCellule.Entete) }).ToList()),
    };

    [Theory]
    [InlineData("Patient.Ins", "Oui")]
    [InlineData("Demandes[].NumeroDemande", "Oui")]
    [InlineData("Demandes[].AccessionNumber", "Oui")]
    [InlineData("Actes[].StudyInstanceUid", "Oui")]
    [InlineData("Custodian.Id", "Oui")]
    [InlineData("SignataireLegal.Professionnel.Id", "Oui")]
    [InlineData("SignataireLegal.Professionnel.Profession", "Non")]
    [InlineData("Auteurs[].Professionnel.Id", "En partie")]
    [InlineData("Actes[].CodeCcam", "Non")]
    [InlineData("Actes[].Executant.Id", "Non")]
    [InlineData("MedecinsDemandeurs[].Professionnel.Id", "Non")]
    public void FlagsFieldsThatMustComeFromRis(string chemin, string attendu)
    {
        Assert.Equal(attendu, ReferentielChamps.Champs.Single(c => c.Chemin == chemin).FourniParRis);
        Assert.All(ReferentielChamps.Champs, c => Assert.Contains(c.FourniParRis, new[] { "Oui", "Non", "En partie" }));
    }

    [Fact]
    public void GeneratesFieldListWorkbook()
    {
        XlsxWriter.Ecrire(XlsxPath, Feuilles());

        // Relecture : une ligne par champ, colonnes attendues, identifiants et noms uniques.
        using var zip = ZipFile.OpenRead(XlsxPath);
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var lignes = XDocument.Load(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open()).Descendants(main + "row")
            .Select(r => r.Elements(main + "c").Select(c => c.Value).ToArray()).ToList();
        Assert.Equal(Colonnes, lignes[0]);
        Assert.Equal(ReferentielChamps.Champs.Count, lignes.Count - 1);
        Assert.All(lignes, l => Assert.Equal(Colonnes.Length, l.Length));
        Assert.All(lignes.Skip(1), l => Assert.All(l, v => Assert.False(string.IsNullOrWhiteSpace(v))));
        Assert.Equal(lignes.Count - 1, lignes.Skip(1).Select(l => l[0]).Distinct().Count());
        Assert.Equal(lignes.Count - 1, lignes.Skip(1).Select(l => l[1]).Distinct().Count());
    }

    [Fact]
    public void WorkbookIsRewrittenOnlyWhenContentChanges()
    {
        XlsxWriter.Ecrire(XlsxPath, Feuilles());

        Assert.False(XlsxWriter.Ecrire(XlsxPath, Feuilles()));
    }
}
