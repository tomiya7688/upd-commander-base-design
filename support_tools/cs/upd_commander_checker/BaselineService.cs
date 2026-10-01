using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace UpdCommanderChecker;

internal sealed record BaselineEntry(
    string Fingerprint,
    string Rule,
    string Path,
    string Symbol,
    string Context,
    string Severity,
    int Line = 0,
    string Message = ""
);

internal sealed record BaselineDocument(
    int SchemaVersion,
    int FingerprintVersion,
    IReadOnlyList<BaselineEntry> Findings
);

internal sealed record BaselineComparison(
    IReadOnlyList<BaselineEntry> New,
    IReadOnlyList<BaselineEntry> Existing,
    IReadOnlyList<BaselineEntry> Resolved
);

internal static partial class BaselineService
{
    private const string Domain = "upd-finding-fingerprint-v1";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    internal static string Fingerprint(string rule, string path, string symbol, string context)
    {
        rule = rule.Normalize(NormalizationForm.FormC).ToUpperInvariant();
        path = CanonicalPath(path);
        symbol = symbol.Normalize(NormalizationForm.FormC);
        context = context.Normalize(NormalizationForm.FormC);
        if (!RulePattern().IsMatch(rule))
            throw new InvalidDataException("rule must match UPD followed by at least three digits");
        if (context.Length == 0)
            throw new InvalidDataException("context must not be empty");
        var fields = new[] { Domain, rule, path, symbol, context };
        if (fields.Any(field => field.Contains('\0')))
            throw new InvalidDataException("identity fields must not contain NUL");
        return "sha256:"
            + Convert
                .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\0', fields))))
                .ToLowerInvariant();
    }

    internal static BaselineEntry FromFinding(Finding finding)
    {
        if (string.IsNullOrEmpty(finding.Context))
            throw new InvalidDataException(
                $"{finding.Code} {finding.Path}: missing stable identity context"
            );
        return new BaselineEntry(
            Fingerprint(finding.Code, finding.Path, finding.Symbol, finding.Context),
            finding.Code,
            CanonicalPath(finding.Path),
            finding.Symbol.Normalize(NormalizationForm.FormC),
            finding.Context.Normalize(NormalizationForm.FormC),
            finding.Severity,
            finding.Line,
            finding.Message
        );
    }

    internal static void Write(string filename, IReadOnlyList<Finding> findings)
    {
        var entries = findings
            .Select(FromFinding)
            .OrderBy(item => item.Fingerprint, StringComparer.Ordinal)
            .ToArray();
        EnsureUnique(entries);
        var document = new BaselineDocument(1, 1, entries);
        var full = Path.GetFullPath(filename);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(
            full,
            JsonSerializer.Serialize(document, JsonOptions) + Environment.NewLine,
            new UTF8Encoding(false)
        );
    }

    internal static BaselineDocument Load(string filename)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(filename));
        var root = json.RootElement;
        RequireKind(root, JsonValueKind.Object, "baseline must be an object");
        var schema = RequiredInt(root, "schema_version");
        var version = RequiredInt(root, "fingerprint_version");
        if (schema != 1)
            throw new InvalidDataException(
                $"unsupported schema_version {schema}; supported version is 1"
            );
        if (version != 1)
            throw new InvalidDataException(
                $"unsupported fingerprint_version {version}; supported version is 1"
            );
        if (
            !root.TryGetProperty("findings", out var items)
            || items.ValueKind != JsonValueKind.Array
        )
            throw new InvalidDataException("findings must be an array");
        var findings = new List<BaselineEntry>();
        foreach (
            var (item, index) in items.EnumerateArray().Select((value, index) => (value, index))
        )
        {
            try
            {
                RequireKind(item, JsonValueKind.Object, "entry must be an object");
                var entry = new BaselineEntry(
                    RequiredString(item, "fingerprint"),
                    RequiredString(item, "rule"),
                    RequiredString(item, "path"),
                    RequiredString(item, "symbol"),
                    RequiredString(item, "context"),
                    RequiredString(item, "severity"),
                    OptionalInt(item, "line"),
                    OptionalString(item, "message")
                );
                Validate(entry, true);
                findings.Add(
                    entry with
                    {
                        Rule = entry.Rule.Normalize(NormalizationForm.FormC).ToUpperInvariant(),
                        Path = CanonicalPath(entry.Path),
                        Symbol = entry.Symbol.Normalize(NormalizationForm.FormC),
                        Context = entry.Context.Normalize(NormalizationForm.FormC),
                    }
                );
            }
            catch (Exception exception)
                when (exception is InvalidDataException or ArgumentException or FormatException)
            {
                throw new InvalidDataException(
                    $"findings[{index}]: {exception.Message}",
                    exception
                );
            }
        }
        EnsureUnique(findings);
        return new BaselineDocument(schema, version, findings);
    }

    internal static BaselineComparison Compare(
        IReadOnlyList<Finding> findings,
        BaselineDocument baseline
    )
    {
        var current = findings.Select(FromFinding).ToArray();
        EnsureUnique(current);
        var old = baseline.Findings.ToDictionary(item => item.Fingerprint, StringComparer.Ordinal);
        var now = current.ToDictionary(item => item.Fingerprint, StringComparer.Ordinal);
        return new BaselineComparison(
            current
                .Where(item => !old.ContainsKey(item.Fingerprint))
                .OrderBy(item => item.Fingerprint)
                .ToArray(),
            current
                .Where(item => old.ContainsKey(item.Fingerprint))
                .OrderBy(item => item.Fingerprint)
                .ToArray(),
            baseline
                .Findings.Where(item => !now.ContainsKey(item.Fingerprint))
                .OrderBy(item => item.Fingerprint)
                .ToArray()
        );
    }

    private static void Validate(BaselineEntry entry, bool verify)
    {
        var fingerprint = Fingerprint(entry.Rule, entry.Path, entry.Symbol, entry.Context);
        if (entry.Severity is not ("error" or "warning" or "attention"))
            throw new InvalidDataException("severity is invalid");
        if (entry.Line < 0)
            throw new InvalidDataException("line must not be negative");
        if (verify && entry.Fingerprint != fingerprint)
            throw new InvalidDataException("fingerprint does not match identity");
    }

    private static string CanonicalPath(string value)
    {
        value = value.Normalize(NormalizationForm.FormC).Replace('\\', '/');
        if (value.StartsWith('/') || Regex.IsMatch(value, "^[A-Za-z]:"))
            throw new InvalidDataException("path must be repository-relative");
        var parts = new List<string>();
        foreach (var part in value.Split('/'))
        {
            if (part == "..")
                throw new InvalidDataException("path must not contain '..'");
            if (part.Length > 0 && part != ".")
                parts.Add(part);
        }
        if (parts.Count == 0)
            throw new InvalidDataException("path must not be empty");
        return string.Join('/', parts);
    }

    private static void EnsureUnique(IEnumerable<BaselineEntry> entries)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
            if (!seen.Add(entry.Fingerprint))
                throw new InvalidDataException("duplicate Finding identity or fingerprint");
    }

    private static string RequiredString(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String
            ? item.GetString()!
            : throw new InvalidDataException($"missing or invalid field '{name}'");

    private static int RequiredInt(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var item) && item.TryGetInt32(out var value)
            ? value
            : throw new InvalidDataException($"missing or invalid field '{name}'");

    private static int OptionalInt(JsonElement obj, string name) =>
        !obj.TryGetProperty(name, out var item) ? 0
        : item.TryGetInt32(out var value) && value > 0 ? value
        : throw new InvalidDataException($"{name} must be a positive integer");

    private static string OptionalString(JsonElement obj, string name) =>
        !obj.TryGetProperty(name, out var item) ? ""
        : item.ValueKind == JsonValueKind.String ? item.GetString()!
        : throw new InvalidDataException($"{name} must be a string");

    private static void RequireKind(JsonElement item, JsonValueKind kind, string message)
    {
        if (item.ValueKind != kind)
            throw new InvalidDataException(message);
    }

    [GeneratedRegex("^UPD[0-9]{3,}$", RegexOptions.CultureInvariant)]
    private static partial Regex RulePattern();
}
