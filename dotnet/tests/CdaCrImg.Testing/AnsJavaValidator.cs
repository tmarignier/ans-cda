using System.Diagnostics;
using System.Xml.Linq;

namespace CdaCrImg.Testing;

/// <summary>
/// Validation par l'outillage Java du kit ANS (schematrons/moteur) : XSD via xsdvalidator-1.3.jar et
/// schématrons ISO compilés en XSLT 2 avec Saxon HE. Équivalent multiplateforme (Windows, Linux, macOS)
/// de tools/validate-cda.sh : ne dépend que de Java (JAVA_HOME ou java dans le PATH).
/// </summary>
public static class AnsJavaValidator
{
    private static readonly XNamespace Svrl = "http://purl.oclc.org/dsdl/svrl";
    private static readonly object CompileLock = new();
    private static readonly Lazy<string?> JavaPath = new(FindJava);

    private static string Moteur => Path.Combine(RepoPaths.Schematrons, "moteur");

    /// <summary>Exécutable Java, ou null s'il est introuvable.</summary>
    public static string? Java => JavaPath.Value;

    /// <summary>Valide le document contre le XSD CDA ; retourne les erreurs (vide si valide).</summary>
    public static IReadOnlyList<string> ValidateXsd(string documentPath)
    {
        var (_, stdout, stderr) = Run("-jar", Path.Combine(Moteur, "xsdvalidator-1.3.jar"), documentPath, RepoPaths.CdaXsd);
        var report = XDocument.Parse(stdout);
        return (string?)report.Root!.Attribute("result") == "OK"
            ? Array.Empty<string>()
            : report.Root.Elements().Select(e => e.Value).DefaultIfEmpty(stderr).ToList();
    }

    /// <summary>
    /// Applique un schématron (chemin relatif à schematrons/, sans extension, ex.
    /// <c>profils/IHE</c>) ; retourne les messages des failed-assert (vide si conforme).
    /// </summary>
    public static IReadOnlyList<string> ValidateSchematron(string documentPath, string schematron)
    {
        var svrlPath = Path.Combine(Path.GetTempPath(), $"cdacrimg-{Guid.NewGuid():N}.svrl");
        try
        {
            Saxon(documentPath, CompiledSchematron(schematron), svrlPath);
            return XDocument.Load(svrlPath).Descendants(Svrl + "failed-assert")
                .Select(f => string.Join(" ", f.Element(Svrl + "text")!.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                             + " @ " + (string?)f.Attribute("location"))
                .ToList();
        }
        finally
        {
            File.Delete(svrlPath);
        }
    }

    /// <summary>
    /// Compile le schématron en XSLT (include → abstract → svrl), mis en cache à côté du .sch :
    /// les chemins relatifs des jeux de valeurs y sont résolus depuis le répertoire du .sch.
    /// Même cache que tools/validate-cda.sh (fichiers .compiled_*.xsl ignorés par git), écrit de façon atomique.
    /// </summary>
    private static string CompiledSchematron(string schematron)
    {
        var sch = Path.Combine(RepoPaths.Schematrons, schematron.Replace('/', Path.DirectorySeparatorChar) + ".sch");
        var xsl = Path.Combine(Path.GetDirectoryName(sch)!, ".compiled_" + Path.GetFileNameWithoutExtension(sch) + ".xsl");
        lock (CompileLock)
        {
            if (File.Exists(xsl) && File.GetLastWriteTimeUtc(xsl) >= File.GetLastWriteTimeUtc(sch))
            {
                return xsl;
            }
            // Plusieurs processus (projets de tests exécutés en parallèle) peuvent compiler en même temps :
            // compilation dans un fichier temporaire du même répertoire, puis renommage atomique.
            var step1 = Path.GetTempFileName();
            var step2 = Path.GetTempFileName();
            var compiled = $"{xsl}.{Guid.NewGuid():N}.tmp";
            try
            {
                Saxon(sch, Path.Combine(Moteur, "iso_dsdl_include.xsl"), step1);
                Saxon(step1, Path.Combine(Moteur, "iso_abstract_expand.xsl"), step2);
                Saxon(step2, Path.Combine(Moteur, "iso_svrl_for_xslt2.xsl"), compiled);
                try
                {
                    File.Move(compiled, xsl, overwrite: true);
                }
                catch (IOException) when (File.Exists(xsl))
                {
                    // Un autre processus vient d'écrire le même cache (fichier ouvert en lecture sous Windows) : on l'utilise.
                }
            }
            finally
            {
                File.Delete(step1);
                File.Delete(step2);
                File.Delete(compiled);
            }
            return xsl;
        }
    }

    private static void Saxon(string source, string stylesheet, string output) =>
        Run("-cp", Path.Combine(Moteur, "saxon9he.jar"), "net.sf.saxon.Transform",
            "-s:" + source, "-xsl:" + stylesheet, "-o:" + output);

    private static (int ExitCode, string Stdout, string Stderr) Run(params string[] args)
    {
        var psi = new ProcessStartInfo(Java ?? throw new InvalidOperationException("Java introuvable."))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }
        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        var result = (process.ExitCode, stdout.Result, stderr.Result);
        if (result.ExitCode != 0 && args[0] == "-cp")
        {
            throw new InvalidOperationException($"Échec Saxon ({string.Join(" ", args.Skip(3))}) :{Environment.NewLine}{result.Item3}");
        }
        return result;
    }

    private static string? FindJava()
    {
        var exe = OperatingSystem.IsWindows() ? "java.exe" : "java";
        var candidates = new List<string>();
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrEmpty(javaHome))
        {
            candidates.Add(Path.Combine(javaHome, "bin", exe));
        }
        candidates.AddRange((Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(dir => Path.Combine(dir.Trim('"'), exe)));
        return candidates.FirstOrDefault(File.Exists);
    }
}

/// <summary>Théorie ignorée (avec message) lorsque Java n'est pas installé.</summary>
public sealed class JavaTheoryAttribute : TheoryAttribute
{
    public JavaTheoryAttribute()
    {
        if (AnsJavaValidator.Java == null)
        {
            Skip = "Java introuvable (JAVA_HOME ou PATH) : validation par les schématrons ANS ignorée.";
        }
    }
}
