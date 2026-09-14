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

    [Fact]
    public void UPD203ResolvesUsingAlias()
    {
        using var project = new TempProject();
        project.Write("process/alias_commander.cs", "using WebClient = System.Net.Http.HttpClient; namespace Sample; internal sealed class AliasCommander { internal void Run() { _ = new WebClient(); } }");
        var findings = project.Scan();
        TestAssert.Has(findings, "UPD203", "error");
    }

    [Fact]
    public void UPD203UsesInferredReceiverType()
    {
        using var project = new TempProject();
        project.Write("process/inferred_commander.cs", "using System.Net.Http; namespace Sample; internal sealed class InferredCommander { internal void Run() { var client = new HttpClient(); _ = client.GetAsync(\"https://example.com\"); } }");
        var findings = project.Scan();
        TestAssert.Has(findings, "UPD203", "error");
    }

    [Fact]
    public void UPD203IgnoresSameNamedUserType()
    {
        using var project = new TempProject();
        project.Write("process/local_commander.cs", "namespace Sample; internal sealed class HttpClient { internal void Send() { } } internal sealed class LocalCommander { internal void Run() { var client = new HttpClient(); client.Send(); } }");
        var findings = project.Scan();
        Assert.DoesNotContain(findings, finding => finding.Code == "UPD203");
    }
}
