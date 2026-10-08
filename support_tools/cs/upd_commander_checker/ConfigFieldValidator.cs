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
        RequireFailOn(new ConfigFieldInput(root, "fail_on"));
        RequireSeverityOverrides(new ConfigFieldInput(root, "severity_overrides"));
        RequireGateExceptions(new ConfigFieldInput(root, "gate_exceptions"));
        RequireCommonRoots(new ConfigFieldInput(root, "common_roots"));
        RequirePositiveInteger(new ConfigFieldInput(root, "upd301_max_inputs"));
        RequirePositiveInteger(new ConfigFieldInput(root, "flat_layer_min_files"));
        RequirePercentInteger(new ConfigFieldInput(root, "flat_layer_min_direct_percent"));
        RequireMinimumInteger(new ConfigFieldInput(root, "model_group_min_items"), 3);
        RequireMinimumInteger(new ConfigFieldInput(root, "model_group_min_occurrences"), 2);
    }

    private static void RequireFailOn(ConfigFieldInput input)
    {
        if (!input.Root.TryGetProperty(input.Name, out var value))
        {
            return;
        }
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new ConfigException($"invalid config field: {input.Name}");
        }
        try
        {
            GatePolicy.NormalizeFailOn(
                value
                    .EnumerateArray()
                    .Select(item =>
                        item.ValueKind == JsonValueKind.String
                            ? item.GetString()!
                            : throw new ConfigException($"invalid config field: {input.Name}")
                    )
            );
        }
        catch (FormatException)
        {
            throw new ConfigException($"invalid config field: {input.Name}");
        }
    }

    private static void RequireSeverityOverrides(ConfigFieldInput input)
    {
        if (!input.Root.TryGetProperty(input.Name, out var value))
        {
            return;
        }
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new ConfigException($"invalid config field: {input.Name}");
        }
        try
        {
            GatePolicy.NormalizeSeverityOverrides(
                value
                    .EnumerateObject()
                    .Select(item => new KeyValuePair<string, string>(
                        item.Name,
                        item.Value.ValueKind == JsonValueKind.String
                            ? item.Value.GetString()!
                            : throw new ConfigException($"invalid config field: {input.Name}")
                    ))
            );
        }
        catch (FormatException)
        {
            throw new ConfigException($"invalid config field: {input.Name}");
        }
    }

    private static void RequireGateExceptions(ConfigFieldInput input)
    {
        if (!input.Root.TryGetProperty(input.Name, out var value))
        {
            return;
        }
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new ConfigException($"invalid config field: {input.Name}");
        }
        try
        {
            var exceptions = value
                .EnumerateArray()
                .Select(item =>
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        throw new ConfigException($"invalid config field: {input.Name}");
                    }
                    var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                    foreach (var field in item.EnumerateObject())
                    {
                        if (!fields.TryAdd(field.Name, field.Value))
                        {
                            throw new ConfigException($"invalid config field: {input.Name}");
                        }
                    }
                    if (
                        fields.Keys.Any(key =>
                            key is not "rule" and not "path" and not "reason" and not "line"
                        )
                        || !fields.TryGetValue("rule", out var rule)
                        || !fields.TryGetValue("path", out var path)
                        || !fields.TryGetValue("reason", out var reason)
                        || rule.ValueKind != JsonValueKind.String
                        || path.ValueKind != JsonValueKind.String
                        || reason.ValueKind != JsonValueKind.String
                    )
                    {
                        throw new ConfigException($"invalid config field: {input.Name}");
                    }
                    int? lineNumber = null;
                    if (fields.TryGetValue("line", out var line))
                    {
                        if (
                            line.ValueKind != JsonValueKind.Number
                            || !int.TryParse(
                                line.GetRawText(),
                                NumberStyles.None,
                                CultureInfo.InvariantCulture,
                                out var parsed
                            )
                            || parsed < 1
                        )
                        {
                            throw new ConfigException($"invalid config field: {input.Name}");
                        }
                        lineNumber = parsed;
                    }
                    return new GateException(
                        rule.GetString()!,
                        path.GetString()!,
                        reason.GetString()!,
                        lineNumber
                    );
                });
            GatePolicy.NormalizeGateExceptions(exceptions);
        }
        catch (FormatException)
        {
            throw new ConfigException($"invalid config field: {input.Name}");
        }
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

    private static void RequireCommonRoots(ConfigFieldInput input)
    {
        if (!input.Root.TryGetProperty(input.Name, out var value))
        {
            return;
        }
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new ConfigException($"invalid config field: {input.Name}");
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                throw new ConfigException($"invalid config field: {input.Name}");
            }
            var root = item.GetString()?.Trim() ?? string.Empty;
            if (
                root.Length == 0
                || root is "." or ".."
                || root.Equals("ui", StringComparison.OrdinalIgnoreCase)
                || root.Equals("process", StringComparison.OrdinalIgnoreCase)
                || root.Equals("data", StringComparison.OrdinalIgnoreCase)
                || root.Contains('/')
                || root.Contains('\\')
            )
            {
                throw new ConfigException($"invalid config field: {input.Name}");
            }
            seen.Add(root);
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
