using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class ExternalDependencyTests
{
    [Fact]
    public void SystemDataDoesNotTriggerUpdLayerRule()
    {
        using var project = new TempProject();
        project.Write(
            "ui/View.cs",
            "using System.Data; namespace Sample.Ui; internal sealed class View { internal DataTable? Table { get; } }"
        );

        Assert.DoesNotContain(project.Scan(), item => item.Code == "UPD101");
    }

    [Fact]
    public void InternalDataTypeStillTriggersUpdLayerRule()
    {
        using var project = new TempProject();
        project.Write(
            "applications/main/data/Repository.cs",
            "namespace Applications.Main.Data; internal sealed class Repository { }"
        );
        project.Write(
            "applications/main/ui/View.cs",
            "using Applications.Main.Data; namespace Applications.Main.Ui; internal sealed class View { internal Repository? Repository { get; } }"
        );

        Assert.Contains(project.Scan(), item => item.Code == "UPD101");
    }
}
