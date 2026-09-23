using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class ModelAttentionTests
{
    [Fact]
    public void ExistingModelFieldsDoNotTrigger()
    {
        using var project = new TempProject();
        project.Write(
            "process/Models.cs",
            "namespace Sample; internal sealed record FirstModel(int id, string name, string email, int age); " +
                "internal sealed record SecondDto(int id, string name, string email, int age); " +
                "internal struct ThirdRecord { public int id; public string name; public string email; public int age; }"
        );

        Assert.DoesNotContain(project.Scan(), item => item.Code == "UPD406");
    }

    [Fact]
    public void RepeatedParameterGroupTriggers()
    {
        using var project = new TempProject();
        project.Write(
            "process/Sample.cs",
            "namespace Sample; internal static class First { internal static void Run(int id, string name, string email) {} } internal static class Second { internal static void Run(int id, string name, string email) {} }"
        );

        var finding = Assert.Single(project.Scan(), item => item.Code == "UPD406");
        Assert.Contains("items=id,name,email", finding.Message);
        Assert.Contains("occurrences=2", finding.Message);
        Assert.Contains("kind=parameters", finding.Message);
    }

    [Fact]
    public void TwoItemsOrderAndCrossLayerDoNotTrigger()
    {
        using var project = new TempProject();
        project.Write(
            "process/Order.cs",
            "namespace Sample; internal static class A { internal static void Pair(int id, string name) {} internal static void One(int id, string name, string email) {} internal static void Two(string email, string name, int id) {} }"
        );
        project.Write(
            "ui/Cross.cs",
            "namespace Sample; internal static class Cross { internal static void One(int id, string name, string email) {} }"
        );

        Assert.DoesNotContain(project.Scan(), item => item.Code == "UPD406");
    }

    [Fact]
    public void TupleAndPerformanceIgnoreBehaveAsSpecified()
    {
        using (var project = new TempProject())
        {
            project.Write(
                "process/TupleCase.cs",
                "namespace Sample; internal static class TupleCase { internal static object One(int x, int y, int z) => (x, y, z); internal static object Two(int a, int b, int c) => (x, y, z); }"
            );
            Assert.Contains(
                project.Scan(),
                item => item.Code == "UPD406" && item.Message.Contains("kind=tuple")
            );
        }

        using (var project = new TempProject())
        {
            project.Write(
                "process/Parallel.cs",
                "namespace Sample; internal static class Parallel { internal static int One(int[] xs, int[] ys, int[] zs, int i) { return xs[i] + ys[i] + zs[i]; } internal static int Two(int[] xs, int[] ys, int[] zs, int i) { return xs[i] + ys[i] + zs[i]; } }"
            );
            project.Write(".updcommanderignore", "UPD406 process/Parallel.cs\n");
            Assert.DoesNotContain(project.Scan(), item => item.Code == "UPD406");
        }
    }

    [Fact]
    public void CustomThresholdsAreApplied()
    {
        using var project = new TempProject();
        project.Write(
            "process/Sample.cs",
            "namespace Sample; internal static class First { internal static void Run(int id, string name, string email) {} } internal static class Second { internal static void Run(int id, string name, string email) {} }"
        );

        Assert.DoesNotContain(project.ScanModel((4, 2)), item => item.Code == "UPD406");
    }
}
