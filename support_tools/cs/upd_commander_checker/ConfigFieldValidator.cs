using System.Text.Json;

namespace UpdCommanderChecker;

internal static class ConfigFieldValidator
{
    internal static void Validate(JsonElement root)
    {
        RequireKind(new ConfigKindFieldInput(root, "input", JsonValueKind.String));
        RequireKind(new ConfigKindFieldInput(root, "output", JsonValueKind.String));
        RequireStringArray(new ConfigFieldInput(root, "ignore"));
        RequireBoolean(new ConfigFieldInput(root, "warnings_as_errors"));
    }

    private static void RequireKind(ConfigKindFieldInput input)
    {
        if (
            input.Root.TryGetProperty(input.Name, out var value)
            && value.ValueKind != input.Expected
        )
        {
            throw new ConfigException($"invalid config field: {input.Name}");
        }
    }

    private static void RequireStringArray(ConfigFieldInput input)
    {
        if (!input.Root.TryGetProperty(input.Name, out var value))
        {
            return;
        }
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new ConfigException($"invalid config field: {input.Name}");
        }
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                throw new ConfigException($"invalid config field: {input.Name}");
            }
        }
    }

    private static void RequireBoolean(ConfigFieldInput input)
    {
        if (!input.Root.TryGetProperty(input.Name, out var value))
        {
            return;
        }
        if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new ConfigException($"invalid config field: {input.Name}");
        }
    }
}
