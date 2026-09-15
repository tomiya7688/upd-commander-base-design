using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class DependencyTests
{
    [Fact]
    public void DependencyRulesHaveExpectedCodes()
    {
        using var project = new TempProject();
        project.Write(
            "data/storage.cs",
            "namespace Data; public static class Storage { public static void Save() {} }\n"
        );
        project.Write(
            "ui/screen_processing.cs",
            "using Data; namespace Sample; public sealed class Screen { public void Run() { Storage.Save(); } }\n"
        );
        project.Write(
            "applications/settings/process/settings_processing.cs",
            "namespace Applications.Settings.Process; public static class SettingsProcessing { public static void Run() {} }\n"
        );
        project.Write(
            "applications/main/process/main_commander.cs",
            "using Applications.Settings.Process; namespace Applications.Main.Process; public sealed class MainCommander { public void Run() { SettingsProcessing.Run(); } }\n"
        );
        project.Write(
            "data/cache_commander.cs",
            "namespace Data; public static class CacheCommander { public static void Run() {} }\n"
        );
        project.Write(
            "data/save_commander.cs",
            "namespace Data; public sealed class SaveCommander { public void Run() { CacheCommander.Run(); } }\n"
        );

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
            "apps/product/applications/profile/process/profile_processing.cs",
            "namespace Apps.Product.Applications.Profile.Process; public static class ProfileProcessing { public static void Run() {} }\n"
        );
        project.Write(
            "apps/product/applications/settings/ui/screen_processing.cs",
            "using Apps.Product.Applications.Profile.Process; namespace Apps.Product.Applications.Settings.Ui; public sealed class Screen { public void Run() { ProfileProcessing.Run(); } }\n"
        );
        TestAssert.Has(project.Scan(), "UPD102", "error");
    }

    [Fact]
    public void FullyQualifiedReferenceIsDetected()
    {
        using var project = new TempProject();
        project.Write(
            "applications/main/data/storage.cs",
            "namespace Applications.Main.Data; public static class Storage { public static void Save() {} }\n"
        );
        project.Write(
            "applications/main/ui/screen.cs",
            "namespace Applications.Main.Ui; public sealed class Screen { public void Run() { Applications.Main.Data.Storage.Save(); } }\n"
        );
        TestAssert.Has(project.Scan(), "UPD101", "error");
    }

    [Fact]
    public void UnusedUsingDoesNotCreateDependency()
    {
        using var project = new TempProject();
        project.Write(
            "applications/settings/data/storage.cs",
            "namespace Applications.Settings.Data; public static class Storage { }\n"
        );
        project.Write(
            "applications/main/ui/screen.cs",
            "using Applications.Settings.Data; namespace Applications.Main.Ui; public sealed class Screen { }\n"
        );

        var findings = project.Scan();
        Assert.DoesNotContain(findings, item => item.Code is "UPD101" or "UPD102" or "UPD103");
    }

    [Fact]
    public void AliasReferenceUsesResolvedSourceSymbol()
    {
        using var project = new TempProject();
        project.Write(
            "applications/main/data/storage.cs",
            "namespace Applications.Main.Data; public static class Storage { public static void Save() {} }\n"
        );
        project.Write(
            "applications/main/ui/screen.cs",
            "using Store = Applications.Main.Data.Storage; namespace Applications.Main.Ui; public sealed class Screen { public void Run() { Store.Save(); } }\n"
        );
        TestAssert.Has(project.Scan(), "UPD101", "error");
    }

    [Fact]
    public void GlobalUsingReferenceIsDetected()
    {
        using var project = new TempProject();
        project.Write("GlobalUsings.cs", "global using Applications.Main.Data;\n");
        project.Write(
            "applications/main/data/storage.cs",
            "namespace Applications.Main.Data; public static class Storage { public static void Save() {} }\n"
        );
        project.Write(
            "applications/main/ui/screen.cs",
            "namespace Applications.Main.Ui; public sealed class Screen { public void Run() { Storage.Save(); } }\n"
        );
        TestAssert.Has(project.Scan(), "UPD101", "error");
    }

    [Fact]
    public void ExternalFrameworkSymbolDoesNotCreateUpdDependency()
    {
        using var project = new TempProject();
        project.Write(
            "ui/screen.cs",
            "using System.Data; namespace Sample; public sealed class Screen { private DataTable? table; }\n"
        );

        var findings = project.Scan();
        Assert.DoesNotContain(findings, item => item.Code is "UPD101" or "UPD102" or "UPD103");
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
