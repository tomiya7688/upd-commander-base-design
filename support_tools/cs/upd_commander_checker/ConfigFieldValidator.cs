using System.Text.Json;

namespace UpdCommanderChecker;

internal static class ConfigFieldValidator
{
    internal static void Validate(JsonElement root)
    {
        RequireKind(root, "input", JsonValueKind.String);
        RequireKind(root, "output", JsonValueKind.String);
        RequireStringArray(root, "ignore");
        RequireBoolean(root, "warnings_as_errors");
    }

    private static void RequireKind(JsonElement root, string name, JsonValueKind expected)
    {
        if (root.TryGetProperty(name, out var value) && value.ValueKind != expected)
        {
            throw new ConfigException($"invalid config field: {name}");
        }
    }

    private static void RequireStringArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return;
        }
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new ConfigException($"invalid config field: {name}");
        }
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                throw new ConfigException($"invalid config field: {name}");
            }
        }
    }

    private static void RequireBoolean(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return;
        }
        if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new ConfigException($"invalid config field: {name}");
        }
    }
}
