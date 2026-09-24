namespace CdaCrImg.Tests;

/// <summary>Accès aux ressources du kit ANS (exemples, XSD, schématrons) présentes à la racine du dépôt.</summary>
internal static class RepoPaths
{
    public static string Root { get; } = FindRoot();

    public static string Examples => Path.Combine(Root, "ExemplesCDA");
    public static string CdaXsd => Path.Combine(Root, "infrastructure", "cda", "CDA_extended.xsd");
    public static string Schematrons => Path.Combine(Root, "schematrons");

    /// <summary>Exemple de référence ANS du CR d'imagerie structuré (niveau 3).</summary>
    public static string CrImgReferenceExample => Path.Combine(Examples, "IMG_CR_IMG_2024.01.xml");

    /// <summary>Exemple ANS de CR d'imagerie non structuré (niveau 1, PDF en base64).</summary>
    public static string CrImgLevel1Example => Path.Combine(Examples, "IMG_CR_IMG_2024.01_CDA-R2-Niveau-1.xml");

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "ExemplesCDA")) &&
                Directory.Exists(Path.Combine(dir.FullName, "schematrons")))
            {
                return dir.FullName;
            }
        }
        throw new DirectoryNotFoundException("Racine du dépôt (ExemplesCDA/, schematrons/) introuvable.");
    }
}
