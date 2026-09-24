using System.Diagnostics;
using CdaCrImg.Serialization;
using Xunit.Abstractions;

namespace CdaCrImg.Tests;

/// <summary>
/// Validation par les schématrons officiels du kit ANS via tools/validate-cda.sh (Java requis).
/// Exclure avec : dotnet test --filter Category!=Schematron
/// </summary>
[Trait("Category", "Schematron")]
public class SchematronTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("profils/structurationMinimale/ASIP-STRUCT-MIN-StrucMin")]
    [InlineData("profils/CI-SIS_ModelesDeContenusCDA")]
    [InlineData("profils/IHE")]
    public void Level1Document_PassesTransverseProfiles(string schematron)
    {
        var file = Path.Combine(Path.GetTempPath(), $"cdacrimg-level1-{Guid.NewGuid():N}.xml");
        File.WriteAllText(file, CrImgWriter.WriteToString(SampleReports.Level1()));
        try
        {
            var (exitCode, log) = RunValidation(file, schematron);
            output.WriteLine(log);
            Assert.True(exitCode == 0, log);
        }
        finally
        {
            File.Delete(file);
        }
    }

    private static (int ExitCode, string Log) RunValidation(string file, string schematron)
    {
        var psi = new ProcessStartInfo("bash")
        {
            ArgumentList = { Path.Combine(RepoPaths.Root, "tools", "validate-cda.sh"), file, schematron },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, stdout.Result + stderr.Result);
    }
}
