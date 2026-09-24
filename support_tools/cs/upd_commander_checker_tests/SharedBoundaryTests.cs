using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class SharedBoundaryTests
{
    [Fact]
    public void LayerMayDependOnCommonEvenWhenPathLooksLikeProcessing()
    {
        using var project = new TempProject();
        project.Write(
            "common/contracts/settings_processing.cs",
            "namespace Shared.Contracts; public sealed class SettingsProcessing { }\n"
        );
        project.Write(
            "ui/messenger/ui_messenger.cs",
            "namespace Ui; public sealed class UiMessenger { private Shared.Contracts.SettingsProcessing? value; }\n"
        );

        Assert.DoesNotContain(
            project.Scan(),
            item => item.Code is "UPD101" or "UPD102" or "UPD103"
        );
    }

    [Fact]
    public void CommonMustNotDependOnLayerSpecificImplementation()
    {
        using var project = new TempProject();
        project.Write(
            "common/contracts/bridge.cs",
            "namespace Shared.Contracts; public sealed class Bridge { public void Run() { Process.Engine.Run(); } }\n"
        );
        project.Write(
            "process/engine.cs",
            "namespace Process; public static class Engine { public static void Run() {} }\n"
        );

        TestAssert.Has(project.Scan(), "UPD101", "error");
    }

    [Fact]
    public void ConfiguredCommonRootsControlSemanticDependencyClassification()
    {
        using var project = new TempProject();
        project.Write(
            "contracts/bridge.cs",
            "namespace Shared.Contracts; public sealed class Bridge { public void Run() { Process.Engine.Run(); } }\n"
        );
        project.Write(
            "process/engine.cs",
            "namespace Process; public static class Engine { public static void Run() {} }\n"
        );

        TestAssert.Has(project.ScanWithCommonRoots(["contracts"]), "UPD101", "error");
        Assert.DoesNotContain(
            project.ScanWithCommonRoots([]),
            item => item.Code == "UPD101"
        );
    }

    [Fact]
    public void SharedProcessingDoesNotBypassApplicationBoundary()
    {
        TestAssert.Has(ScanTarget("shared/process/settings_processing.cs"), "UPD102", "error");
    }

    [Fact]
    public void SharedDataDoesNotBypassApplicationBoundary()
    {
        TestAssert.Has(ScanTarget("shared/data/storage.cs"), "UPD102", "error");
    }

    [Fact]
    public void SharedContractsRemainBoundaryApi()
    {
        Assert.DoesNotContain(
            ScanTarget("shared/contracts/process/settings_processing.cs"),
            item => item.Code == "UPD102"
        );
    }

    [Fact]
    public void SharedMessagesRemainBoundaryApi()
    {
        Assert.DoesNotContain(
            ScanTarget("shared/messages/process/settings_processing.cs"),
            item => item.Code == "UPD102"
        );
    }

    private static List<Finding> ScanTarget(string targetSuffix)
    {
        using var project = new TempProject();
        project.Write(
            $"applications/settings/{targetSuffix}",
            "namespace BoundaryTarget; public static class Api { public static void Run() {} }\n"
        );
        project.Write(
            "applications/main/process/main_commander.cs",
            "namespace MainApplication; public sealed class MainCommander { public void Run() { BoundaryTarget.Api.Run(); } }\n"
        );
        return project.Scan();
    }
}
