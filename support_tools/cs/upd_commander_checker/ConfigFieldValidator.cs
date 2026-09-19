using System.Globalization;
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
        RequirePositiveInteger(new ConfigFieldInput(root, "upd301_max_inputs"));
        RequirePositiveInteger(new ConfigFieldInput(root, "flat_layer_min_files"));
        RequirePercentInteger(new ConfigFieldInput(root, "flat_layer_min_direct_percent"));
        RequireMinimumInteger(new ConfigFieldInput(root, "model_group_min_items"), 3);
        RequireMinimumInteger(new ConfigFieldInput(root, "model_group_min_occurrences"), 2);
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

    private static void RequirePositiveInteger(ConfigFieldInput input)
    {
        if (!input.Root.TryGetProperty(input.Name, out var value))
        {
            return;
        }
        if (
            value.ValueKind != JsonValueKind.Number
            || !int.TryParse(
                value.GetRawText(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var parsed
            )
            || parsed < 1
        )
        {
            throw new ConfigException($"invalid config field: {input.Name}");
        }
    }

    private static void RequirePercentInteger(ConfigFieldInput input)
    {
        if (!input.Root.TryGetProperty(input.Name, out var value))
        {
            return;
        }
        if (
            value.ValueKind != JsonValueKind.Number
            || !int.TryParse(
                value.GetRawText(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var parsed
            )
            || parsed < 1
            || parsed > 100
        )
        {
            throw new ConfigException($"invalid config field: {input.Name}");
        }
    }

    private static void RequireMinimumInteger(ConfigFieldInput input, int minimum)
    {
        if (!input.Root.TryGetProperty(input.Name, out var value))
        {
            return;
        }
        if (
            value.ValueKind != JsonValueKind.Number
            || !int.TryParse(
                value.GetRawText(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var parsed
            )
            || parsed < minimum
        )
        {
            throw new ConfigException($"invalid config field: {input.Name}");
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
