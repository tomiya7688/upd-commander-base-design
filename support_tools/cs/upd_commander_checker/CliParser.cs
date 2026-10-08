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
        var failOn = config.FailOn is null
            ? new List<string>()
            : new List<string>(config.FailOn);
        var failOnConfigured = config.FailOn is not null;
        var severityOverrides = new Dictionary<string, string>(
            config.SeverityOverrides,
            StringComparer.Ordinal
        );
        var targetSpecified = false;

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
            else if (argument == "--warnings-as-errors")
            {
                warningsAsErrors = true;
            }
            else if (argument == "--attentions-as-errors")
            {
                attentionsAsErrors = true;
            }
            else if (argument == "--fail-on")
            {
                if (
                    index + 1 >= args.Length
                    || args[index + 1].StartsWith("-", StringComparison.Ordinal)
                )
                {
                    throw new CliUsageException($"missing value for {argument}");
                }
                try
                {
                    failOn = GatePolicy.ParseFailOnArgument(args[++index]);
                }
                catch (FormatException exception)
                {
                    throw new CliUsageException($"invalid value for --fail-on: {exception.Message}");
                }
                failOnConfigured = true;
            }
            else if (argument == "--severity-override")
            {
                if (
                    index + 1 >= args.Length
                    || args[index + 1].StartsWith("-", StringComparison.Ordinal)
                )
                {
                    throw new CliUsageException($"missing value for {argument}");
                }
                try
                {
                    var parsed = GatePolicy.ParseSeverityOverrideArgument(args[++index]);
                    severityOverrides[parsed.Key] = parsed.Value;
                }
                catch (FormatException exception)
                {
                    throw new CliUsageException(
                        $"invalid value for --severity-override: {exception.Message}"
                    );
                }
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

        return new CliOptions(
            target,
            output,
            ignores,
            warningsAsErrors,
            attentionsAsErrors,
            failOn,
            failOnConfigured,
            severityOverrides
        );
    }
}
