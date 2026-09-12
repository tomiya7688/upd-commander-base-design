namespace UpdCommanderChecker;

internal static class Program
{
    private static int Main(string[] args)
    {
        var config = ConfigLoader.Load();
        var target = config.Input;
        var output = config.Output;
        var ignores = new List<string>(config.Ignore);
        var warningsAsErrors = config.WarningsAsErrors;

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
            else
            {
                target = args[index];
            }
        }

        if (!File.Exists(target) && !Directory.Exists(target))
        {
            return Finish(new[] { $"E UPD000 {target} missing" }, output, 2);
        }

        var findings = Scanner.ScanPath(target, ignores);
        var errors = 0;
        var warnings = 0;
        var lines = new List<string>();
        foreach (var finding in findings)
        {
            var warning = finding.Severity == "warning";
            lines.Add($"{(warning ? "W" : "E")} {finding.Code} {finding.Path}:{finding.Line} {finding.Message}");
            if (warning)
            {
                warnings++;
            }
            else
            {
                errors++;
            }
        }

        if (errors > 0 || (warningsAsErrors && warnings > 0))
        {
            lines.Add($"FAIL e={errors} w={warnings}");
            return Finish(lines, output, 1);
        }
        lines.Add(warnings > 0 ? $"OK w={warnings}" : "OK");
        return Finish(lines, output, 0);
    }

    private static int Finish(IEnumerable<string> lines, string output, int exitCode)
    {
        var values = lines.ToList();
        foreach (var line in values)
        {
            Console.WriteLine(line);
        }
        if (!string.IsNullOrWhiteSpace(output))
        {
            var parent = Path.GetDirectoryName(output);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }
            File.WriteAllLines(output, values);
        }
        return exitCode;
    }
}
