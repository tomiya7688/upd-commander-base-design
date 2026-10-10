using System.Text.Json;

namespace UpdCommanderChecker;

// {
// 責務: [Program: C# Checkerのprocess entryと終了code決定を担当する]
// フィールド: [なし]
// 処理: [1: CLIを解釈する, 2: scan・baseline・gateを実行する]
// }
internal static class Program
{
    // {
    // 責務: [Main: Checkerを実行しCLI結果に対応する終了codeを返す]
    // 処理: [1: configと引数を読む, 2: Findingをscan・比較する, 3: gate結果を出力する]
    // 引数: [args: process起動引数]
    // 戻り値: [成功0、Finding gate失敗1、設定・実行エラー2]
    // }
    private static int Main(string[] args)
    {
        CheckerConfig config;
        try
        {
            config = ConfigLoader.Load();
        }
        catch (ConfigException exception)
        {
            Console.WriteLine($"CONFIG ERROR: {exception.Message}");
            return 2;
        }

        CliOptions options;
        try
        {
            options = CliParser.Parse(new CliParseInput(args, config));
        }
        catch (CliUsageException exception)
        {
            Console.WriteLine($"USAGE ERROR: {exception.Message}");
            return 2;
        }

        if (!File.Exists(options.Target) && !Directory.Exists(options.Target))
        {
            return ReportOutput.Finish(
                new FinishInput(new[] { $"E UPD000 {options.Target} missing" }, options.Output, 2)
            );
        }

        var selectedFindings = RuleSelection.Filter(
            new RuleSelectionInput(
                Scanner.ScanPath(
                    new ScanPathInput(
                        options.Target,
                        options.Ignores,
                        config.Upd301MaxInputs,
                        config.FlatLayerMinFiles,
                        config.FlatLayerMinDirectPercent,
                        config.ModelGroupMinItems,
                        config.ModelGroupMinOccurrences,
                        config.CommonRoots
                    )
                ),
                config.EnabledRules
            )
        );
        var findings = GatePolicy.ApplySeverityOverrides(
            selectedFindings,
            options.SeverityOverrides
        );
        Dictionary<string, string> statuses = new(StringComparer.Ordinal);
        IReadOnlyList<BaselineEntry> resolved = [];
        try
        {
            if (options.WriteBaseline)
            {
                var target = Path.GetFullPath(options.Target);
                var root = Directory.Exists(target) ? target : Path.GetDirectoryName(target)!;
                var output =
                    options.WriteBaselinePath.Length > 0
                        ? options.WriteBaselinePath
                        : Path.Combine(root, ".upd-baseline.json");
                BaselineService.Write(output, findings);
            }
            else if (options.BaselinePath.Length > 0)
            {
                var comparison = BaselineService.Compare(
                    findings,
                    BaselineService.Load(options.BaselinePath)
                );
                foreach (var finding in comparison.New)
                    statuses.Add(finding.Fingerprint, "NEW");
                foreach (var finding in comparison.Existing)
                    statuses.Add(finding.Fingerprint, "EXISTING");
                resolved = comparison.Resolved;
            }
        }
        catch (Exception exception)
            when (exception
                    is IOException
                        or UnauthorizedAccessException
                        or InvalidDataException
                        or ArgumentException
                        or JsonException
            )
        {
            return ReportOutput.Finish(
                new FinishInput([$"BASELINE ERROR: {exception.Message}"], options.Output, 2)
            );
        }
        var errors = 0;
        var warnings = 0;
        var attentions = 0;
        var lines = new List<string>();
        var gateFindings = new List<Finding>();
        foreach (var finding in findings)
        {
            var level = "A";
            if (finding.Severity == "error")
            {
                level = "E";
                errors++;
            }
            else if (finding.Severity == "warning")
            {
                level = "W";
                warnings++;
            }
            else
            {
                attentions++;
            }
            var line = $"{level} {finding.Code} {finding.Path}:{finding.Line} {finding.Message}";
            var reason = GatePolicy.GateExceptionReason(finding, config.GateExceptions);
            var suffix = reason is null ? "" : $" [gate exception: {reason}]";
            var fingerprint = "";
            if (options.BaselinePath.Length > 0)
            {
                try
                {
                    fingerprint = BaselineService.FromFinding(finding).Fingerprint;
                    line = $"{statuses[fingerprint]} {line}";
                }
                catch (Exception exception)
                    when (exception
                            is KeyNotFoundException
                                or InvalidDataException
                                or ArgumentException
                    )
                {
                    return ReportOutput.Finish(
                        new FinishInput([$"BASELINE ERROR: {exception.Message}"], options.Output, 2)
                    );
                }
            }
            if (
                reason is null
                && (
                    options.FailOnScope == "all" || statuses.GetValueOrDefault(fingerprint) == "NEW"
                )
            )
            {
                gateFindings.Add(finding);
            }
            lines.Add(line + suffix);
        }
        foreach (var finding in resolved)
        {
            var level =
                finding.Severity == "error" ? "E"
                : finding.Severity == "warning" ? "W"
                : "A";
            lines.Add(
                $"RESOLVED {level} {finding.Rule} {finding.Path}:{(finding.Line > 0 ? finding.Line.ToString() : "?")} {finding.Message}"
            );
        }

        var failOn = options.FailOnConfigured ? options.FailOn : LegacyFailOn(options);
        var failed = GatePolicy.ShouldFail(gateFindings, failOn);
        if (failed)
        {
            lines.Add($"FAIL e={errors} w={warnings} a={attentions}");
            return ReportOutput.Finish(new FinishInput(lines, options.Output, 1));
        }
        lines.Add(warnings > 0 || attentions > 0 ? $"OK w={warnings} a={attentions}" : "OK");
        return ReportOutput.Finish(new FinishInput(lines, options.Output, 0));
    }

    // {
    // 責務: [LegacyFailOn: fail_on未指定時に従来のseverity gateを再現する]
    // 処理: [1: errorを含める, 2: legacy flagに応じwarning/attentionを追加する]
    // 引数: [options: CLI gate設定]
    // 戻り値: [失敗対象severity一覧]
    // }
    private static IReadOnlyList<string> LegacyFailOn(CliOptions options)
    {
        var failOn = new List<string> { "error" };
        if (options.WarningsAsErrors)
        {
            failOn.Add("warning");
        }
        if (options.AttentionsAsErrors)
        {
            failOn.Add("attention");
        }
        return failOn;
    }
}
