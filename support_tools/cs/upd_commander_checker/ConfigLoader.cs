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
            using var document = JsonDocument.Parse(text);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ConfigException($"invalid config: {path}");
            }
            var enabledRules = RuleSelection.ReadEnabledRules(document.RootElement);
            var config = JsonSerializer.Deserialize<CheckerConfig>(text)
                ?? throw new ConfigException($"invalid config: {path}");
            config.EnabledRules = enabledRules;
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
        catch (ConfigException)
        {
            throw;
        }
        catch (IOException)
        {
            throw new ConfigException($"invalid config: {path}");
        }
        catch (UnauthorizedAccessException)
        {
            throw new ConfigException($"invalid config: {path}");
        }
        catch (JsonException)
        {
            throw new ConfigException($"invalid config: {path}");
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
