using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class GatePolicyTests
{
    [Fact]
    public void NormalizesGateAndOverrideInputs()
    {
        Assert.Equal(
            ["error", "warning"],
            GatePolicy.NormalizeFailOn([" ERROR ", "warning", "error"])
        );
        Assert.Empty(GatePolicy.ParseFailOnArgument(""));

        var parsed = GatePolicy.ParseSeverityOverrideArgument("upd203=WARNING");
        Assert.Equal("UPD203", parsed.Key);
        Assert.Equal("warning", parsed.Value);
    }

    [Fact]
    public void RejectsUnsupportedGateValuesAndRuleIds()
    {
        Assert.Throws<FormatException>(() => GatePolicy.NormalizeFailOn(["fatal"]));
        Assert.Throws<FormatException>(() =>
            GatePolicy.NormalizeSeverityOverrides([
                new KeyValuePair<string, string>("bad", "error"),
            ])
        );
        Assert.Throws<FormatException>(() =>
            GatePolicy.ParseSeverityOverrideArgument("UPD101 warning")
        );
    }

    [Fact]
    public void OverridesKeepFindingsAndGateUsesEffectiveSeverity()
    {
        var finding = new Finding("src/a.cs", 4, "UPD203", "direct I/O", "error");
        var effective = GatePolicy.ApplySeverityOverrides(
            [finding],
            new Dictionary<string, string>(StringComparer.Ordinal) { ["UPD203"] = "warning" }
        );

        Assert.Single(effective);
        Assert.Equal("error", finding.Severity);
        Assert.Equal("warning", effective[0].Severity);
        Assert.False(GatePolicy.ShouldFail(effective, ["error"]));
        Assert.True(GatePolicy.ShouldFail(effective, ["warning"]));
        Assert.False(GatePolicy.ShouldFail(effective, []));
    }

    [Fact]
    public void GateExceptionMatchesRulePathAndOptionalLine()
    {
        var finding = new Finding("src/a.cs", 4, "UPD203", "direct I/O", "error");
        var exceptions = GatePolicy.NormalizeGateExceptions([
            new GateException("upd203", "src/a.cs", "approved", 4),
            new GateException("UPD203", "src/a.cs", "file allowance", null),
            new GateException("UPD203", "src/b.cs", "file allowance", null),
        ]);

        Assert.Equal("approved", GatePolicy.GateExceptionReason(finding, exceptions));
        Assert.Null(GatePolicy.GateExceptionReason(finding with { Line = 5 }, exceptions.Take(1)));
        Assert.Equal(
            "file allowance",
            GatePolicy.GateExceptionReason(finding with { Line = 5 }, exceptions)
        );
        Assert.True(GatePolicy.ShouldFail([finding], ["error"]));
        Assert.Throws<FormatException>(() =>
            GatePolicy.NormalizeGateExceptions([
                new GateException("UPD203", "../a.cs", "invalid", null),
            ])
        );
    }
}
