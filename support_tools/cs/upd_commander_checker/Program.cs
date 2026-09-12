namespace UpdCommanderChecker;

internal static class Program
{
    private static int Main(string[] args)
    {
        var target = ".";
        var ignores = new List<string>();
        var warningsAsErrors = false;

        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--ignore" && index + 1 < args.Length)
            {
                ignores.Add(args[++index]);
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
            Console.WriteLine($"E UPD000 {target} missing");
            return 2;
        }

        var findings = Scanner.ScanPath(target, ignores);
        var errors = 0;
        var warnings = 0;
        foreach (var finding in findings)
        {
            var warning = finding.Severity == "warning";
            Console.WriteLine($"{(warning ? "W" : "E")} {finding.Code} {finding.Path}:{finding.Line} {finding.Message}");
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
            Console.WriteLine($"FAIL e={errors} w={warnings}");
            return 1;
        }
        Console.WriteLine(warnings > 0 ? $"OK w={warnings}" : "OK");
        return 0;
    }
}
