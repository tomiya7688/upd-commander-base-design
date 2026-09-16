using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class SingleFileScanTests
{
    [Fact]
    public void SingleFileScanPreservesUiLayer()
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

        var findings = Scanner.ScanPath(
            new ScanPathInput(project.PathFor("applications/main/ui/View.cs"), [])
        );

        Assert.Contains(findings, item => item.Code == "UPD101");
    }

    [Fact]
    public void SingleFileScanPreservesApplicationBoundary()
    {
        using var project = new TempProject();
        project.Write(
            "applications/settings/process/SettingsProcessing.cs",
            "namespace Applications.Settings.Process; internal static class SettingsProcessing { internal static void Run() { } }"
        );
        project.Write(
            "applications/main/process/MainCommander.cs",
            "using Applications.Settings.Process; namespace Applications.Main.Process; internal sealed class MainCommander { internal void Run() => SettingsProcessing.Run(); }"
        );

        var findings = Scanner.ScanPath(
            new ScanPathInput(project.PathFor("applications/main/process/MainCommander.cs"), [])
        );

        Assert.Contains(findings, item => item.Code == "UPD102");
    }
}
