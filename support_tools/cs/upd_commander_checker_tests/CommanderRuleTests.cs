using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class CommanderRuleTests
{
    [Fact]
    public void CommanderRulesHaveExpectedCodes()
    {
        using var project = new TempProject();
        project.Write("process/rule_commander.cs", "using System.Net.Http; namespace Sample; internal sealed class RuleCommander { internal void Run() { for (var i = 0; i < 2; i++) { } var value = 1 + 2; _ = new HttpClient(); } }");
        var findings = project.Scan();
        TestAssert.Has(findings, "UPD201", "warning");
        TestAssert.Has(findings, "UPD202", "warning");
        TestAssert.Has(findings, "UPD203", "error");
    }
}
