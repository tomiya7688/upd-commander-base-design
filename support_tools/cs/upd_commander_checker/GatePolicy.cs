using System.Text.RegularExpressions;

namespace UpdCommanderChecker;

internal static class GatePolicy
{
    private static readonly HashSet<string> SupportedSeverities = new(StringComparer.Ordinal)
    {
        "error",
        "warning",
        "attention",
    };

    private static readonly Regex RulePattern = new(
        @"^UPD[0-9]{3,}$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    );

    internal static List<string> NormalizeFailOn(IEnumerable<string> values)
    {
        var result = new List<string>();
        foreach (var value in values)
        {
            var severity = value.Trim().ToLowerInvariant();
            if (!SupportedSeverities.Contains(severity))
            {
                throw new FormatException("contains an unsupported severity");
            }
            if (!result.Contains(severity, StringComparer.Ordinal))
            {
                result.Add(severity);
            }
        }
        return result;
    }

    internal static List<string> ParseFailOnArgument(string value)
    {
        return value.Length == 0 ? new List<string>() : NormalizeFailOn(value.Split(','));
    }

    internal static Dictionary<string, string> NormalizeSeverityOverrides(
        IEnumerable<KeyValuePair<string, string>> values
    )
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (rule, rawSeverity) in values)
        {
            var normalizedRule = rule.ToUpperInvariant();
            var severity = rawSeverity.ToLowerInvariant();
            if (!RulePattern.IsMatch(normalizedRule))
            {
                throw new FormatException("contains an invalid UPD rule");
            }
            if (!SupportedSeverities.Contains(severity))
            {
                throw new FormatException("contains an unsupported severity");
            }
            result[normalizedRule] = severity;
        }
        return result;
    }

    internal static KeyValuePair<string, string> ParseSeverityOverrideArgument(string value)
    {
        var separator = value.IndexOf('=');
        if (separator < 0)
        {
            throw new FormatException("expected UPDnnn=severity");
        }
        var normalized = NormalizeSeverityOverrides([
            new KeyValuePair<string, string>(value[..separator], value[(separator + 1)..]),
        ]);
        return normalized.Single();
    }

    internal static List<Finding> ApplySeverityOverrides(
        IEnumerable<Finding> findings,
        IReadOnlyDictionary<string, string> overrides
    )
    {
        return findings
            .Select(finding =>
                overrides.TryGetValue(finding.Code.ToUpperInvariant(), out var severity)
                    ? finding with
                    {
                        Severity = severity,
                    }
                    : finding
            )
            .ToList();
    }

    internal static bool ShouldFail(IEnumerable<Finding> findings, IEnumerable<string> failOn)
    {
        var gate = new HashSet<string>(failOn, StringComparer.Ordinal);
        return findings.Any(finding => gate.Contains(finding.Severity));
    }
}
