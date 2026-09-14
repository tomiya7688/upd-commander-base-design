using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class ResponsibilityRuleTests
{
    [Fact]
    public void ResponsibilityRulesHaveExpectedCodes()
    {
        using var project = new TempProject();
        var methods = string.Join("\n", Enumerable.Range(1, 13).Select(index => $"internal void M{index}() {{ }}"));
        project.Write("process/large_processing.cs", $"namespace Sample; internal sealed class LargeProcessing {{ {methods} }}");
        project.Write("process/mixed_processing.cs", "namespace Sample; internal sealed class First { internal void Run() { } } internal sealed class Second { internal void Run() { } }");
        var findings = project.Scan();
        TestAssert.Has(findings, "UPD401", "warning");
        TestAssert.Has(findings, "UPD402", "warning");
    }
}
