using System.Globalization;
using System.Text.Json;

namespace UpdCommanderChecker;

internal static class ConfigGateExceptionValidator
{
    internal static void Validate(ConfigFieldInput input)
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
}
