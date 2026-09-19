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

        var findings = RuleSelection.Filter(
            new RuleSelectionInput(
                Scanner.ScanPath(
                    new ScanPathInput(
                        options.Target,
                        options.Ignores,
                        config.Upd301MaxInputs,
                        config.FlatLayerMinFiles,
                        config.FlatLayerMinDirectPercent,
                        config.ModelGroupMinItems,
                        config.ModelGroupMinOccurrences
                    )
                ),
                config.EnabledRules
            )
        );
        var errors = 0;
        var warnings = 0;
        var attentions = 0;
        var lines = new List<string>();
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
            lines.Add($"{level} {finding.Code} {finding.Path}:{finding.Line} {finding.Message}");
        }

        var failed =
            errors > 0
            || (options.WarningsAsErrors && warnings > 0)
            || (options.AttentionsAsErrors && attentions > 0);
        if (failed)
        {
            lines.Add($"FAIL e={errors} w={warnings} a={attentions}");
            return ReportOutput.Finish(new FinishInput(lines, options.Output, 1));
        }
        lines.Add(warnings > 0 || attentions > 0 ? $"OK w={warnings} a={attentions}" : "OK");
        return ReportOutput.Finish(new FinishInput(lines, options.Output, 0));
    }
}
