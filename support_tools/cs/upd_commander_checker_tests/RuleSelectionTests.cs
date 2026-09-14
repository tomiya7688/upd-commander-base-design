using System.Text.Json;
using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class RuleSelectionTests
{
    [Fact]
    public void MissingSelectionKeepsAllFindings()
    {
        var findings = new[] { new Finding("sample.cs", 1, "UPD101", "message") };
        Assert.Single(RuleSelection.Filter(new RuleSelectionInput(findings, null)));
    }

    [Fact]
    public void EmptySelectionDisablesAllFindings()
    {
        var findings = new[] { new Finding("sample.cs", 1, "UPD101", "message") };
        Assert.Empty(RuleSelection.Filter(new RuleSelectionInput(findings, [])));
    }

    [Fact]
    public void SelectedRulesFilterFindings()
    {
        var findings = new[]
        {
            new Finding("first.cs", 1, "UPD101", "message"),
            new Finding("second.cs", 2, "UPD202", "message", "warning")
        };
        var filtered = RuleSelection.Filter(new RuleSelectionInput(findings, ["UPD202"]));
        Assert.Equal("UPD202", Assert.Single(filtered).Code);
    }

    [Fact]
    public void UnknownRuleIsRejected()
    {
        Assert.False(RuleSelection.AreSupported(["UPD999"]));
    }

    [Fact]
    public void ConfigRuleArrayIsRead()
    {
        using var document = JsonDocument.Parse("{\"enabled_rules\":[\"UPD101\"]}");
        Assert.Equal(["UPD101"], RuleSelection.ReadEnabledRules(document.RootElement));
    }

    [Fact]
    public void NullConfigRuleArrayIsRejected()
    {
        using var document = JsonDocument.Parse("{\"enabled_rules\":null}");
        Assert.Throws<ConfigException>(() => RuleSelection.ReadEnabledRules(document.RootElement));
    }
}
