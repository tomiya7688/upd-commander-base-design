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
}
