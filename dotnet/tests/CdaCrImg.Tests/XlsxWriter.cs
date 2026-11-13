using System.IO.Compression;
using System.Security;
using System.Text;

namespace CdaCrImg.Tests;

/// <summary>Style d'une cellule du classeur produit par <see cref="XlsxWriter"/>.</summary>
internal enum StyleCellule
{
    Defaut = 0,
    Entete = 1,
    Texte = 2,
    TexteGras = 3,
    Code = 4,
    Oui = 5,
    Non = 6,
    Conditionnel = 7,
}

/// <summary>Feuille : nom, largeurs de colonnes, lignes (la première est l'en-tête, figée et filtrable).</summary>
internal sealed record FeuilleXlsx(string Nom, double[] Largeurs, IReadOnlyList<(string Texte, StyleCellule Style)[]> Lignes);

/// <summary>
/// Écriture minimale d'un classeur Excel (OpenXML SpreadsheetML, sans dépendance) : chaînes en ligne, police
/// Arial, en-tête figé avec filtre, impression A4 paysage ajustée à la largeur. Le contenu est déterministe :
/// <see cref="Ecrire"/> ne réécrit le fichier que si l'une de ses parties change.
/// </summary>
internal static class XlsxWriter
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    /// <summary>Écrit le classeur s'il est absent ou différent ; retourne vrai s'il a été (ré)écrit.</summary>
    public static bool Ecrire(string chemin, IReadOnlyList<FeuilleXlsx> feuilles)
    {
        var parties = Parties(feuilles);
        if (File.Exists(chemin) && Identique(chemin, parties)) return false;

        using var flux = File.Create(chemin);
        using var zip = new ZipArchive(flux, ZipArchiveMode.Create);
        foreach (var (nom, contenu) in parties)
        {
            var entree = zip.CreateEntry(nom, CompressionLevel.Optimal);
            entree.LastWriteTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            using var ecriture = entree.Open();
            var octets = new UTF8Encoding(false).GetBytes(contenu);
            ecriture.Write(octets, 0, octets.Length);
        }
        return true;
    }

    /// <summary>Parties du classeur (chemin dans l'archive → XML).</summary>
    public static IReadOnlyList<(string Nom, string Contenu)> Parties(IReadOnlyList<FeuilleXlsx> feuilles)
    {
        var parties = new List<(string, string)>
        {
            ("[Content_Types].xml", Xml(
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">"
                + "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>"
                + "<Default Extension=\"xml\" ContentType=\"application/xml\"/>"
                + "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>"
                + "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>"
                + string.Concat(feuilles.Select((_, i) =>
                    $"<Override PartName=\"/xl/worksheets/sheet{i + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"))
                + "</Types>")),
            ("_rels/.rels", Xml(
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
                + $"<Relationship Id=\"rId1\" Type=\"{Rel}/officeDocument\" Target=\"xl/workbook.xml\"/>"
                + "</Relationships>")),
            ("xl/workbook.xml", Xml(
                $"<workbook xmlns=\"{Main}\" xmlns:r=\"{Rel}\"><sheets>"
                + string.Concat(feuilles.Select((f, i) => $"<sheet name=\"{Esc(f.Nom)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>"))
                + "</sheets><definedNames>"
                + string.Concat(feuilles.Select((f, i) =>
                    $"<definedName name=\"_xlnm._FilterDatabase\" localSheetId=\"{i}\" hidden=\"1\">'{Esc(f.Nom)}'!$A$1:${Colonne(f.Largeurs.Length - 1)}${f.Lignes.Count}</definedName>"
                    + $"<definedName name=\"_xlnm.Print_Titles\" localSheetId=\"{i}\">'{Esc(f.Nom)}'!$1:$1</definedName>"))
                + "</definedNames></workbook>")),
            ("xl/_rels/workbook.xml.rels", Xml(
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
                + string.Concat(feuilles.Select((_, i) =>
                    $"<Relationship Id=\"rId{i + 1}\" Type=\"{Rel}/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>"))
                + $"<Relationship Id=\"rId{feuilles.Count + 1}\" Type=\"{Rel}/styles\" Target=\"styles.xml\"/>"
                + "</Relationships>")),
            ("xl/styles.xml", Styles()),
        };
        parties.AddRange(feuilles.Select((f, i) => ($"xl/worksheets/sheet{i + 1}.xml", Feuille(f))));
        return parties;
    }

    private static string Feuille(FeuilleXlsx f)
    {
        var plage = $"A1:{Colonne(f.Largeurs.Length - 1)}{f.Lignes.Count}";
        var xml = new StringBuilder($"<worksheet xmlns=\"{Main}\" xmlns:r=\"{Rel}\">")
            .Append("<sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr>")
            .Append($"<dimension ref=\"{plage}\"/>")
            .Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>")
            .Append("<sheetFormatPr defaultRowHeight=\"15\"/><cols>");
        for (var i = 0; i < f.Largeurs.Length; i++)
            xml.Append($"<col min=\"{i + 1}\" max=\"{i + 1}\" width=\"{f.Largeurs[i].ToString(System.Globalization.CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>");
        xml.Append("</cols><sheetData>");
        for (var r = 0; r < f.Lignes.Count; r++)
        {
            xml.Append($"<row r=\"{r + 1}\">");
            for (var c = 0; c < f.Lignes[r].Length; c++)
            {
                var (texte, style) = f.Lignes[r][c];
                xml.Append($"<c r=\"{Colonne(c)}{r + 1}\" s=\"{(int)style}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Esc(texte)}</t></is></c>");
            }
            xml.Append("</row>");
        }
        return Xml(xml.Append("</sheetData>")
            .Append($"<autoFilter ref=\"{plage}\"/>")
            .Append("<pageMargins left=\"0.4\" right=\"0.4\" top=\"0.5\" bottom=\"0.5\" header=\"0.3\" footer=\"0.3\"/>")
            .Append("<pageSetup paperSize=\"9\" orientation=\"landscape\" fitToWidth=\"1\" fitToHeight=\"0\"/>")
            .Append("</worksheet>").ToString());
    }

    /// <summary>Styles, dans l'ordre de <see cref="StyleCellule"/>.</summary>
    private static string Styles()
    {
        const string arial = "<sz val=\"10\"/><name val=\"Arial\"/><family val=\"2\"/>";
        string Fond(string rgb) => $"<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF{rgb}\"/><bgColor indexed=\"64\"/></patternFill></fill>";
        string Xf(int font, int fill, int border, string alignement) =>
            $"<xf numFmtId=\"0\" fontId=\"{font}\" fillId=\"{fill}\" borderId=\"{border}\" xfId=\"0\""
            + (font > 0 ? " applyFont=\"1\"" : "") + (fill > 0 ? " applyFill=\"1\"" : "") + (border > 0 ? " applyBorder=\"1\"" : "")
            + $" applyAlignment=\"1\"><alignment {alignement} wrapText=\"1\"/></xf>";
        const string haut = "vertical=\"top\"";
        return Xml($"<styleSheet xmlns=\"{Main}\">"
            + "<fonts count=\"4\">"
            + $"<font>{arial}</font>"
            + $"<font><b/><color rgb=\"FFFFFFFF\"/>{arial}</font>"
            + $"<font><b/>{arial}</font>"
            + "<font><sz val=\"10\"/><name val=\"Consolas\"/><family val=\"3\"/></font>"
            + "</fonts>"
            + "<fills count=\"6\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>"
            + Fond("1F5FA8") + Fond("FDECEA") + Fond("EAF4EC") + Fond("FFF4DC") + "</fills>"
            + "<borders count=\"2\"><border><left/><right/><top/><bottom/><diagonal/></border>"
            + "<border><left/><right/><top/><bottom style=\"thin\"><color rgb=\"FFD6DAE1\"/></bottom><diagonal/></border></borders>"
            + "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>"
            + "<cellXfs count=\"8\">"
            + "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>"
            + Xf(1, 2, 0, "vertical=\"center\"")
            + Xf(0, 0, 1, haut)
            + Xf(2, 0, 1, haut)
            + Xf(3, 0, 1, haut)
            + Xf(0, 3, 1, haut)
            + Xf(0, 4, 1, haut)
            + Xf(0, 5, 1, haut)
            + "</cellXfs>"
            + "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>"
            + "</styleSheet>");
    }

    private static bool Identique(string chemin, IReadOnlyList<(string Nom, string Contenu)> parties)
    {
        try
        {
            using var zip = ZipFile.OpenRead(chemin);
            if (zip.Entries.Count != parties.Count) return false;
            foreach (var (nom, contenu) in parties)
            {
                var entree = zip.GetEntry(nom);
                if (entree == null) return false;
                using var lecture = new StreamReader(entree.Open(), Encoding.UTF8);
                if (lecture.ReadToEnd() != contenu) return false;
            }
            return true;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private static string Xml(string contenu) => "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" + contenu;

    private static string Esc(string texte) => SecurityElement.Escape(texte)!;

    private static string Colonne(int index) => ((char)('A' + index)).ToString();
}
