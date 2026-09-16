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
            CliParser.Parse([argument], new CheckerConfig())
        );
        Assert.Contains(message, error.Message);
    }

    [Fact]
    public void OptionCannotConsumeAnotherOptionAsValue()
    {
        var error = Assert.Throws<CliUsageException>(() =>
            CliParser.Parse(["--output", "--warnings-as-errors"], new CheckerConfig())
        );
        Assert.Contains("missing value", error.Message);
    }

    [Fact]
    public void MultipleTargetsFail()
    {
        var error = Assert.Throws<CliUsageException>(() =>
            CliParser.Parse(["first", "second"], new CheckerConfig())
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
            [
                "--ignore",
                "generated/**",
                "--output",
                "report.txt",
                "--warnings-as-errors",
                "source",
            ],
            config
        );

        Assert.Equal("source", options.Target);
        Assert.Equal("report.txt", options.Output);
        Assert.Equal(["old/**", "generated/**"], options.Ignores);
        Assert.True(options.WarningsAsErrors);
    }
}
