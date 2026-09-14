using System.Text.Json;

namespace UpdCommanderChecker;

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
            config.Input = Resolve(new ResolvePathInput(
                root,
                string.IsNullOrWhiteSpace(config.Input) ? "." : config.Input));
            if (!string.IsNullOrWhiteSpace(config.Output))
            {
                config.Output = Resolve(new ResolvePathInput(root, config.Output));
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

    private static string Resolve(ResolvePathInput input)
    {
        return Path.IsPathRooted(input.Value)
            ? input.Value
            : Path.GetFullPath(Path.Combine(input.Root, input.Value));
    }
}
