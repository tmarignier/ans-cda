using CdaToHtml;

const string AppName = "CdaToHtml";
const string Version = "1.0.0";

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintHelp();
    return 0;
}

if (args[0] is "-v" or "--version")
{
    Console.WriteLine($"{AppName} v{Version}");
    return 0;
}

if (args.Length < 3)
{
    Console.Error.WriteLine("Erreur : arguments insuffisants.");
    Console.Error.WriteLine();
    PrintHelp();
    return 1;
}

string cdaPath  = args[0];
string xslPath  = args[1];
string htmlPath = args[2];

try
{
    Console.WriteLine($"CDA  : {cdaPath}");
    Console.WriteLine($"XSL  : {xslPath}");
    Console.WriteLine($"HTML : {htmlPath}");
    Console.WriteLine("Transformation en cours...");

    CdaHtmlGenerator.GenerateHtml(cdaPath, xslPath, htmlPath);

    Console.WriteLine($"Succès ! Fichier HTML généré : {Path.GetFullPath(htmlPath)}");
    return 0;
}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine($"Fichier introuvable : {ex.FileName}");
    return 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Erreur lors de la transformation : {ex.Message}");
    return 3;
}

static void PrintHelp()
{
    Console.WriteLine($"""
        {AppName} v{Version} — Transforme un fichier CDA en HTML via une feuille de style XSL

        Usage :
          CdaToHtml <cda.xml> <feuille-de-style.xsl> <sortie.html>

        Arguments :
          cda.xml              Fichier CDA source (XML HL7 CDA R2)
          feuille-de-style.xsl Feuille de style XSL à appliquer
          sortie.html          Fichier HTML à générer

        Options :
          -h, --help     Affiche cette aide
          -v, --version  Affiche la version

        Exemples :
          CdaToHtml doc.xml CDA-FO.xsl output.html
          CdaToHtml BIO-CR-BIO_2024.01.xml cda_CRBIO.xsl resultat.html
        """);
}
