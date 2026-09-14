using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class ContainerRuleTests
{
    [Fact]
    public void ContainerRulesHaveExpectedCodes()
    {
        using var project = new TempProject();
        project.Write(
            "process/container_processing.cs",
            "namespace Sample; internal sealed class ContainerProcessing { internal (int Left, int Right) Split(int left, int right) => (left, right); }"
        );
        var findings = project.Scan();
        TestAssert.Has(findings, "UPD301", "attention");
        TestAssert.Has(findings, "UPD302", "attention");
    }

    [Fact]
    public void SubstantialCommanderCompressionIsWarning()
    {
        using var project = new TempProject();
        project.Write(
            "process/large_commander.cs",
            "namespace Sample; internal sealed class LargeCommander { internal void Run(\nint a,\nint b,\nint c,\nint d,\nint e,\nint f,\nint g,\nint h,\nint i,\nint j,\nint k,\nint l) { } }"
        );
        TestAssert.Has(project.Scan(), "UPD303", "warning");
    }
}
