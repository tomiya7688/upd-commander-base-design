using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace UpdCommanderChecker;

// {
// 責務: [BaselineService: Finding identityの生成、baselineの保存・検証・比較を担う]
// フィールド: [Domain/JsonOptions: fingerprint契約とJSON出力設定]
// 処理: [1: Findingを共通v1契約へ正規化する, 2: baselineを読み書きし差分を分類する]
// }
internal static partial class BaselineService // upd: ignore UPD401 - schema orchestration spans the baseline contract
{
    private const string Domain = "upd-finding-fingerprint-v1";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    // {
    // 責務: [Fingerprint: canonical identityから共有v1 fingerprintを生成する]
    // 処理: [1: 4 identity fieldを正規化する, 2: domain separator付きSHA-256を計算する]
    // 引数: [rule/path/symbol/context: identityを構成する値]
    // 戻り値: [sha256:形式のfingerprint]
    // エラー: [rule・path・contextまたはNUL制約に違反した場合]
    // }
    internal static string Fingerprint( // upd: ignore UPD301 - identity fields are the shared fingerprint contract
        string rule,
        string path,
        string symbol,
        string context
    )
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

    // {
    // 責務: [FromFinding: 通常Findingをbaseline entryへ変換する]
    // 処理: [1: stable contextを確認する, 2: canonical identityと表示metadataを格納する]
    // 引数: [finding: 変換対象]
    // 戻り値: [baseline entry]
    // エラー: [stable contextがない、またはidentityが不正な場合]
    // }
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

    // {
    // 責務: [Write: Finding一覧から決定的なbaseline JSONを保存する]
    // 処理: [1: entryを構築して重複を検証する, 2: fingerprint順に整列する, 3: UTF-8 JSONを書き込む]
    // 引数: [filename: 保存先, findings: 記録するFinding一覧]
    // 戻り値: [なし]
    // 副作用: [親directoryとbaselineファイルを作成する]
    // }
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

    // {
    // 責務: [Load: baseline JSONを読み込み契約に従って検証する]
    // 処理: [1: JSON objectとversionを確認する, 2: 各entryを検証・正規化する, 3: fingerprint重複を拒否する]
    // 引数: [filename: 読み込むファイル]
    // 戻り値: [検証済みBaselineDocument]
    // エラー: [読込、schema、entryの検証に失敗した場合]
    // }
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

    // {
    // 責務: [Compare: 現在のFindingとbaselineを差分分類する]
    // 処理: [1: 現在のentryを構築する, 2: fingerprint集合を比較する]
    // 引数: [findings: 現在のFinding一覧, baseline: 比較対象]
    // 戻り値: [NEW/EXISTING/RESOLVED各Findingの一覧]
    // エラー: [現在のFindingに同一identityが重複する場合]
    // }
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

    // {
    // 責務: [Validate: entryのseverity・lineと再計算fingerprintを検証する]
    // 処理: [1: identityを再計算する, 2: metadataとfingerprintの一致を確認する]
    // 引数: [entry: 検証対象, verify: 保存済みfingerprintを照合するか]
    // 戻り値: [なし]
    // エラー: [metadataまたはfingerprintが契約に反する場合]
    // }
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

    // {
    // 責務: [CanonicalPath: repository相対pathをNFC・slash形式へ正規化する]
    // 処理: [1: 区切りとUnicodeを正規化する, 2: 絶対pathや親参照を拒否する]
    // 引数: [value: 正規化するpath]
    // 戻り値: [canonical path]
    // エラー: [絶対path、親参照、空pathの場合]
    // }
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

    // {
    // 責務: [EnsureUnique: entry内のfingerprint重複を検出する]
    // 処理: [1: fingerprintを集合へ登録し、重複を拒否する]
    // 引数: [entries: 検査するentry一覧]
    // 戻り値: [なし]
    // エラー: [同一fingerprintが複数存在する場合]
    // }
    private static void EnsureUnique(IEnumerable<BaselineEntry> entries)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
            if (!seen.Add(entry.Fingerprint))
                throw new InvalidDataException("duplicate Finding identity or fingerprint");
    }

    // {
    // 責務: [RequiredString: JSON objectから必須文字列fieldを読む]
    // 処理: [1: fieldの存在と文字列型を確認する]
    // 引数: [obj: 読み取り元, name: field名]
    // 戻り値: [fieldの文字列値]
    // エラー: [field欠落または型不一致の場合]
    // }
    private static string RequiredString(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String
            ? item.GetString()!
            : throw new InvalidDataException($"missing or invalid field '{name}'");

    // {
    // 責務: [RequiredInt: JSON objectから必須整数fieldを読む]
    // 処理: [1: fieldの存在と整数型を確認する]
    // 引数: [obj: 読み取り元, name: field名]
    // 戻り値: [fieldの整数値]
    // エラー: [field欠落または型不一致の場合]
    // }
    private static int RequiredInt(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var item) && item.TryGetInt32(out var value)
            ? value
            : throw new InvalidDataException($"missing or invalid field '{name}'");

    // {
    // 責務: [OptionalInt: JSON objectから任意の正整数fieldを読む]
    // 処理: [1: field欠落は既定値0とし、存在時は正整数を確認する]
    // 引数: [obj: 読み取り元, name: field名]
    // 戻り値: [fieldの整数値または0]
    // エラー: [fieldが正整数でない場合]
    // }
    private static int OptionalInt(JsonElement obj, string name) =>
        !obj.TryGetProperty(name, out var item) ? 0
        : item.TryGetInt32(out var value) && value > 0 ? value
        : throw new InvalidDataException($"{name} must be a positive integer");

    // {
    // 責務: [OptionalString: JSON objectから任意文字列fieldを読む]
    // 処理: [1: field欠落は空文字とし、存在時は文字列型を確認する]
    // 引数: [obj: 読み取り元, name: field名]
    // 戻り値: [fieldの文字列値または空文字]
    // エラー: [fieldが文字列でない場合]
    // }
    private static string OptionalString(JsonElement obj, string name) =>
        !obj.TryGetProperty(name, out var item) ? ""
        : item.ValueKind == JsonValueKind.String ? item.GetString()!
        : throw new InvalidDataException($"{name} must be a string");

    // {
    // 責務: [RequireKind: JSON値が期待するkindか検証する]
    // 処理: [1: 実際のkindを期待値と比較する]
    // 引数: [item: 検査する値, kind: 期待するkind, message: 不一致時の診断]
    // 戻り値: [なし]
    // エラー: [kindが一致しない場合]
    // }
    private static void RequireKind( // upd: ignore UPD301 - validator needs value, expected kind, and diagnostic
        JsonElement item,
        JsonValueKind kind,
        string message
    )
    {
        if (item.ValueKind != kind)
            throw new InvalidDataException(message);
    }

    // {
    // 責務: [RulePattern: UPDと3桁以上の数字からなるrule codeを識別する]
    // 処理: [1: culture非依存の正規表現を生成する]
    // 引数: [なし]
    // 戻り値: [rule code検証用Regex]
    // }
    [GeneratedRegex("^UPD[0-9]{3,}$", RegexOptions.CultureInvariant)]
    private static partial Regex RulePattern();
}
