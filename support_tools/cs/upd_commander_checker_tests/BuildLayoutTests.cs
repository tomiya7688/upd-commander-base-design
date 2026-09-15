using System.Text.Json;

namespace UpdCommanderChecker.Tests;

public sealed class BuildLayoutTests
{
    [Fact]
    public void BuildOutputContainsConfigPathJson()
    {
        var outputDirectory = FindCheckerBuildOutput();
        var assemblyPath = Path.Combine(outputDirectory, "upd-commander-check.dll");
        var configDirectory = Path.Combine(outputDirectory, "config");
        var configPath = Path.Combine(configDirectory, "path.json");

        Assert.True(File.Exists(assemblyPath), $"Missing checker assembly: {assemblyPath}");
        Assert.True(
            Directory.Exists(configDirectory),
            $"Missing config directory: {configDirectory}"
        );
        Assert.True(File.Exists(configPath), $"Missing config/path.json: {configPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(configPath));
        var root = document.RootElement;
        Assert.Equal(".", root.GetProperty("input").GetString());
        Assert.Equal(JsonValueKind.Array, root.GetProperty("enabled_rules").ValueKind);
    }

    private static string FindCheckerBuildOutput()
    {
        var frameworkDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        var configurationDirectory = frameworkDirectory.Parent!;
        var binDirectory = configurationDirectory.Parent!;
        var testProjectDirectory = binDirectory.Parent!;
        var csDirectory = testProjectDirectory.Parent!;

        return Path.Combine(
            csDirectory.FullName,
            "upd_commander_checker",
            "bin",
            configurationDirectory.Name,
            frameworkDirectory.Name
        );
    }
}
