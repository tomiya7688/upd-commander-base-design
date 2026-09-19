using System.Text.Json;

namespace UpdCommanderChecker;

internal static class RuleSelection
{
    internal static readonly IReadOnlyList<string> SupportedRules =
    [
        "UPD001",
        "UPD002",
        "UPD101",
        "UPD102",
        "UPD103",
        "UPD201",
        "UPD202",
        "UPD203",
        "UPD301",
        "UPD302",
        "UPD303",
        "UPD401",
        "UPD402",
        "UPD403",
        "UPD404",
        "UPD405",
        "UPD406",
    ];

    private static readonly HashSet<string> SupportedRuleSet = new(
        SupportedRules,
        StringComparer.Ordinal
    );

    internal static List<string>? ReadEnabledRules(JsonElement root)
    {
        if (!root.TryGetProperty("enabled_rules", out var property))
        {
            return null;
        }
        if (
            property.ValueKind != JsonValueKind.Array
            || property.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String)
        )
        {
            throw new ConfigException("invalid config field: enabled_rules");
        }

        var rules = property.EnumerateArray().Select(item => item.GetString()!).ToList();
        if (!AreSupported(rules))
        {
            throw new ConfigException("invalid config field: enabled_rules");
        }
        return rules;
    }

    internal static bool AreSupported(IEnumerable<string> rules)
    {
        return rules.All(SupportedRuleSet.Contains);
    }

    internal static List<Finding> Filter(RuleSelectionInput input)
    {
        if (input.EnabledRules is null)
        {
            return input.Findings.ToList();
        }

        var enabled = new HashSet<string>(input.EnabledRules, StringComparer.Ordinal);
        return input.Findings.Where(finding => enabled.Contains(finding.Code)).ToList();
    }
}
