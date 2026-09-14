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

        var target = config.Input;
        var output = config.Output;
        var ignores = new List<string>(config.Ignore);
        var warningsAsErrors = config.WarningsAsErrors;
        var attentionsAsErrors = false;

        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--ignore" && index + 1 < args.Length)
            {
                ignores.Add(args[++index]);
            }
            else if (args[index] == "--output" && index + 1 < args.Length)
            {
                output = args[++index];
            }
            else if (args[index] == "--warnings-as-errors")
            {
                warningsAsErrors = true;
            }
            else if (args[index] == "--attentions-as-errors")
            {
                attentionsAsErrors = true;
            }
            else
            {
                target = args[index];
            }
        }

        if (!File.Exists(target) && !Directory.Exists(target))
        {
            return Finish(new FinishInput(new[] { $"E UPD000 {target} missing" }, output, 2));
        }

        var findings = RuleSelection.Filter(
            new RuleSelectionInput(
                Scanner.ScanPath(new ScanPathInput(target, ignores)),
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
            || (warningsAsErrors && warnings > 0)
            || (attentionsAsErrors && attentions > 0);
        if (failed)
        {
            lines.Add($"FAIL e={errors} w={warnings} a={attentions}");
            return Finish(new FinishInput(lines, output, 1));
        }
        lines.Add(warnings > 0 || attentions > 0 ? $"OK w={warnings} a={attentions}" : "OK");
        return Finish(new FinishInput(lines, output, 0));
    }

    private static int Finish(FinishInput input)
    {
        var values = input.Lines.ToList();
        foreach (var line in values)
        {
            Console.WriteLine(line);
        }
        if (!string.IsNullOrWhiteSpace(input.Output))
        {
            var parent = Path.GetDirectoryName(input.Output);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }
            File.WriteAllLines(input.Output, values);
        }
        return input.ExitCode;
    }
}
