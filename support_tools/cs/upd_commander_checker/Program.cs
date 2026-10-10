using System.Text.Json;

namespace UpdCommanderChecker;

internal static class Program
{
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
            if (reason is null)
            {
                gateFindings.Add(finding);
            }
            if (options.BaselinePath.Length > 0)
            {
                try
                {
                    line = $"{statuses[BaselineService.FromFinding(finding).Fingerprint]} {line}";
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
