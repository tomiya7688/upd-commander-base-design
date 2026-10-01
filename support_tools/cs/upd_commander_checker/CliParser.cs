namespace UpdCommanderChecker;

internal static class CliParser
{
    internal static CliOptions Parse(CliParseInput input)
    {
        var args = input.Args;
        var config = input.Config;
        var target = config.Input;
        var output = config.Output;
        var ignores = new List<string>(config.Ignore);
        var warningsAsErrors = config.WarningsAsErrors;
        var attentionsAsErrors = false;
        var targetSpecified = false;
        var writeBaseline = false;
        var writeBaselinePath = "";
        var baselinePath = "";

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument == "--ignore")
            {
                if (
                    index + 1 >= args.Length
                    || args[index + 1].StartsWith("-", StringComparison.Ordinal)
                )
                {
                    throw new CliUsageException($"missing value for {argument}");
                }
                ignores.Add(args[++index]);
            }
            else if (argument == "--output")
            {
                if (
                    index + 1 >= args.Length
                    || args[index + 1].StartsWith("-", StringComparison.Ordinal)
                )
                {
                    throw new CliUsageException($"missing value for {argument}");
                }
                output = args[++index];
            }
            else if (argument == "--write-baseline")
            {
                writeBaseline = true;
            }
            else if (argument.StartsWith("--write-baseline=", StringComparison.Ordinal))
            {
                writeBaseline = true;
                writeBaselinePath = argument["--write-baseline=".Length..];
            }
            else if (argument == "--baseline")
            {
                if (
                    index + 1 >= args.Length
                    || args[index + 1].StartsWith("-", StringComparison.Ordinal)
                )
                    throw new CliUsageException("missing value for --baseline");
                baselinePath = args[++index];
            }
            else if (argument.StartsWith("--baseline=", StringComparison.Ordinal))
            {
                baselinePath = argument["--baseline=".Length..];
                if (baselinePath.Length == 0)
                    throw new CliUsageException("missing value for --baseline");
            }
            else if (argument == "--warnings-as-errors")
            {
                warningsAsErrors = true;
            }
            else if (argument == "--attentions-as-errors")
            {
                attentionsAsErrors = true;
            }
            else if (argument.StartsWith("-", StringComparison.Ordinal))
            {
                throw new CliUsageException($"unknown option: {argument}");
            }
            else if (targetSpecified)
            {
                throw new CliUsageException($"multiple targets: {argument}");
            }
            else
            {
                target = argument;
                targetSpecified = true;
            }
        }

        if (writeBaseline && baselinePath.Length > 0)
            throw new CliUsageException("--write-baseline and --baseline are mutually exclusive");
        return new CliOptions(
            target,
            output,
            ignores,
            warningsAsErrors,
            attentionsAsErrors,
            writeBaseline,
            writeBaselinePath,
            baselinePath
        );
    }
}
