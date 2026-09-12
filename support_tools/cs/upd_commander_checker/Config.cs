using System.Text.Json;
using System.Text.Json.Serialization;

namespace UpdCommanderChecker;

internal sealed class CheckerConfig
{
    [JsonPropertyName("input")]
    public string Input { get; set; } = ".";

    [JsonPropertyName("output")]
    public string Output { get; set; } = "";

    [JsonPropertyName("ignore")]
    public List<string> Ignore { get; set; } = new();

    [JsonPropertyName("warnings_as_errors")]
    public bool WarningsAsErrors { get; set; }
}

internal static class ConfigLoader
{
    public static CheckerConfig Load()
    {
        var path = FindConfigPath();
        if (path is null)
        {
            return new CheckerConfig();
        }

        try
        {
            var text = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<CheckerConfig>(text) ?? new CheckerConfig();
            var root = Directory.GetParent(Path.GetDirectoryName(path)!)!.FullName;
            config.Input = Resolve(root, string.IsNullOrWhiteSpace(config.Input) ? "." : config.Input);
            if (!string.IsNullOrWhiteSpace(config.Output))
            {
                config.Output = Resolve(root, config.Output);
            }
            return config;
        }
        catch (IOException)
        {
            return new CheckerConfig();
        }
        catch (JsonException)
        {
            return new CheckerConfig();
        }
    }

    private static string? FindConfigPath()
    {
        var executableConfig = Path.Combine(AppContext.BaseDirectory, "config", "path.json");
        if (File.Exists(executableConfig))
        {
            return executableConfig;
        }

        var currentConfig = Path.Combine(Environment.CurrentDirectory, "config", "path.json");
        return File.Exists(currentConfig) ? currentConfig : null;
    }

    private static string Resolve(string root, string value)
    {
        return Path.IsPathRooted(value) ? value : Path.GetFullPath(Path.Combine(root, value));
    }
}
