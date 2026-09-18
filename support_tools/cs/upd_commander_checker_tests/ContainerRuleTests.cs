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
            "namespace Sample; internal sealed class ContainerProcessing { internal (int Left, int Right) Split(int left, int right, int mode) => (left, right); }"
        );
        var findings = project.Scan();
        TestAssert.Has(findings, "UPD301", "attention");
        TestAssert.Has(findings, "UPD302", "attention");
    }

    [Fact]
    public void PrivateMethodIsChecked()
    {
        using var project = new TempProject();
        project.Write(
            "process/private_processing.cs",
            "namespace Sample; internal sealed class PrivateProcessing { private (int Left, int Right) Run(int left, int right, int mode) => (left, right); }"
        );
        var findings = project.Scan();
        TestAssert.Has(findings, "UPD301", "attention");
        TestAssert.Has(findings, "UPD302", "attention");
    }

    [Fact]
    public void DefaultThresholdAllowsTwoAndFlagsThreeInputs()
    {
        using var project = new TempProject();
        project.Write(
            "process/threshold_processing.cs",
            "namespace Sample; internal sealed class ThresholdProcessing { internal void Good(int left, int right) { } internal void Bad(int left, int right, int mode) { } }"
        );

        var findings = project.Scan();

        Assert.Single(findings.Where(item => item.Code == "UPD301"));
    }

    [Fact]
    public void CustomThresholdChangesBoundary()
    {
        using var project = new TempProject();
        project.Write(
            "process/custom_processing.cs",
            "namespace Sample; internal sealed class CustomProcessing { internal void Two(int left, int right) { } internal void Three(int left, int right, int mode) { } internal void Four(int left, int right, int mode, int extra) { } }"
        );

        Assert.Equal(3, project.Scan(upd301MaxInputs: 1).Count(item => item.Code == "UPD301"));
        Assert.Single(project.Scan(upd301MaxInputs: 3).Where(item => item.Code == "UPD301"));
    }

    [Fact]
    public void ParamsParameterCountsAsOneInput()
    {
        using var project = new TempProject();
        project.Write(
            "process/params_processing.cs",
            "namespace Sample; internal sealed class ParamsProcessing { internal void Good(int left, params int[] values) { } internal void Bad(int left, int right, params int[] values) { } }"
        );

        Assert.Single(project.Scan().Where(item => item.Code == "UPD301"));
    }

    [Fact]
    public void ExtensionReceiverDoesNotCountAsInput()
    {
        using var project = new TempProject();
        project.Write(
            "process/extension_processing.cs",
            "namespace Sample; internal static class ExtensionProcessing { internal static void Good(this string value, int left, int right) { } }"
        );

        Assert.DoesNotContain(project.Scan(), item => item.Code == "UPD301");
    }

    [Fact]
    public void ConstructorIsNotChecked()
    {
        using var project = new TempProject();
        project.Write(
            "process/constructor_processing.cs",
            "namespace Sample; internal sealed class ConstructorProcessing { internal ConstructorProcessing(int left, int right) { } }"
        );
        var findings = project.Scan();
        Assert.DoesNotContain(findings, item => item.Code == "UPD301");
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
