using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class DependencyTests
{
    [Fact]
    public void DependencyRulesHaveExpectedCodes()
    {
        using var project = new TempProject();
        project.Write("ui/screen_processing.cs", "using Data.Storage;\nnamespace Sample;\n");
        project.Write(
            "applications/main/process/main_commander.cs",
            "using Applications.Settings.Process.SettingsProcessing;\nnamespace Sample;\n"
        );
        project.Write("data/save_commander.cs", "using Data.CacheCommander;\nnamespace Sample;\n");
        var findings = project.Scan();
        TestAssert.Has(findings, "UPD101", "error");
        TestAssert.Has(findings, "UPD102", "error");
        TestAssert.Has(findings, "UPD103", "warning");
    }

    [Fact]
    public void NestedApplicationDependencyUsesNearestBoundary()
    {
        using var project = new TempProject();
        project.Write(
            "apps/product/applications/settings/ui/screen_processing.cs",
            "using Apps.Product.Applications.Profile.Process.ProfileProcessing;\nnamespace Sample;\n"
        );
        TestAssert.Has(project.Scan(), "UPD102", "error");
    }

    [Fact]
    public void NestedApplicationClassifierUsesNearestScope()
    {
        var module = Classifier.ClassifyPath(
            "apps/product/ui/commander/applications/settings/data/screen.cs"
        );
        Assert.Equal("settings", module.ApplicationId);
        Assert.Equal("data", module.Layer);
        Assert.Equal(string.Empty, module.Role);

        var dependency = Classifier.ClassifyReference(
            "Apps.Product.Ui.Commander.Applications.Profile.Process.ProfileProcessing"
        );
        Assert.Equal("profile", dependency.ApplicationId);
        Assert.Equal("process", dependency.Layer);
        Assert.Equal("processing", dependency.Role);
    }
}
