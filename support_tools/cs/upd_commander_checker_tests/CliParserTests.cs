using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class CliParserTests
{
    [Theory]
    [InlineData("--warning-as-errors", "unknown option")]
    [InlineData("--ignore", "missing value")]
    [InlineData("--output", "missing value")]
    public void InvalidOptionUsageFails(string argument, string message)
    {
        var error = Assert.Throws<CliUsageException>(() =>
            CliParser.Parse(new CliParseInput([argument], new CheckerConfig()))
        );
        Assert.Contains(message, error.Message);
    }

    [Fact]
    public void OptionCannotConsumeAnotherOptionAsValue()
    {
        var error = Assert.Throws<CliUsageException>(() =>
            CliParser.Parse(
                new CliParseInput(["--output", "--warnings-as-errors"], new CheckerConfig())
            )
        );
        Assert.Contains("missing value", error.Message);
    }

    [Fact]
    public void MultipleTargetsFail()
    {
        var error = Assert.Throws<CliUsageException>(() =>
            CliParser.Parse(new CliParseInput(["first", "second"], new CheckerConfig()))
        );
        Assert.Contains("multiple targets", error.Message);
    }

    [Fact]
    public void ValidArgumentsOverrideConfig()
    {
        var config = new CheckerConfig
        {
            Input = "configured",
            Output = "configured.txt",
            Ignore = ["old/**"],
        };
        var options = CliParser.Parse(
            new CliParseInput(
                [
                    "--ignore",
                    "generated/**",
                    "--output",
                    "report.txt",
                    "--warnings-as-errors",
                    "source",
                ],
                config
            )
        );

        Assert.Equal("source", options.Target);
        Assert.Equal("report.txt", options.Output);
        Assert.Equal(["old/**", "generated/**"], options.Ignores);
        Assert.True(options.WarningsAsErrors);
    }

    [Fact]
    public void GateOptionsOverrideConfig()
    {
        var config = new CheckerConfig
        {
            FailOn = ["error"],
            SeverityOverrides = new Dictionary<string, string> { ["UPD203"] = "error" },
        };
        var options = CliParser.Parse(
            new CliParseInput(
                [
                    "--fail-on",
                    "warning,attention",
                    "--severity-override",
                    "upd203=warning",
                    ".",
                ],
                config
            )
        );

        Assert.True(options.FailOnConfigured);
        Assert.Equal(["warning", "attention"], options.FailOn);
        Assert.Equal("warning", options.SeverityOverrides["UPD203"]);
    }

    [Fact]
    public void EmptyFailOnArgumentExplicitlyDisablesFindingGate()
    {
        var options = CliParser.Parse(
            new CliParseInput(["--fail-on", ""], new CheckerConfig())
        );

        Assert.True(options.FailOnConfigured);
        Assert.Empty(options.FailOn);
    }

    [Theory]
    [InlineData("--fail-on", "fatal")]
    [InlineData("--severity-override", "UPD101=fatal")]
    [InlineData("--severity-override", "bad=error")]
    public void InvalidGateOptionsAreRejected(string option, string value)
    {
        var error = Assert.Throws<CliUsageException>(() =>
            CliParser.Parse(new CliParseInput([option, value], new CheckerConfig()))
        );

        Assert.Contains(option, error.Message);
    }
}
