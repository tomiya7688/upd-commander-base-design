using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class FlatLayerTests
{
    [Fact]
    public void DefaultBoundaryMatchesContract()
    {
        using var below = new TempProject();
        WriteFiles(below, 9, 3, 0);
        Assert.DoesNotContain(below.Scan(), finding => finding.Code == "UPD405");

        using var over = new TempProject();
        WriteFiles(over, 10, 2, 0);
        var finding = Assert.Single(over.Scan(), finding => finding.Code == "UPD405");
        Assert.Equal("process", finding.Path);
        Assert.Equal("attention", finding.Severity);
    }

    [Fact]
    public void SmallAndGeneratedHeavyDoNotTrigger()
    {
        using var small = new TempProject();
        WriteFiles(small, 8, 0, 0);
        Assert.DoesNotContain(small.Scan(), finding => finding.Code == "UPD405");

        using var generated = new TempProject();
        WriteFiles(generated, 5, 0, 20);
        Assert.DoesNotContain(generated.Scan(), finding => finding.Code == "UPD405");
    }

    [Fact]
    public void CustomThresholdsAreApplied()
    {
        using var project = new TempProject();
        WriteFiles(project, 6, 0, 0);

        Assert.Contains(
            project.Scan(flatLayerMinFiles: 6, flatLayerMinDirectPercent: 80),
            finding => finding.Code == "UPD405"
        );
    }

    private static void WriteFiles(TempProject project, int direct, int nested, int excluded)
    {
        for (var index = 0; index < direct; index++)
        {
            project.Write(
                $"process/direct_{index}.cs",
                $"namespace Sample; internal static class Direct{index} {{ }}"
            );
        }
        for (var index = 0; index < nested; index++)
        {
            project.Write(
                $"process/group/nested_{index}.cs",
                $"namespace Sample; internal static class Nested{index} {{ }}"
            );
        }
        for (var index = 0; index < excluded; index++)
        {
            project.Write(
                $"process/generated/generated_{index}.cs",
                $"namespace Sample; internal static class Generated{index} {{ }}"
            );
        }
    }
}
