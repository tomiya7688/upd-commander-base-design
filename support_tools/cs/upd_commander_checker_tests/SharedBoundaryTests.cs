using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class SharedBoundaryTests
{
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
        Assert.DoesNotContain(ScanTarget("shared/contracts/process/settings_processing.cs"), item => item.Code == "UPD102");
    }

    [Fact]
    public void SharedMessagesRemainBoundaryApi()
    {
        Assert.DoesNotContain(ScanTarget("shared/messages/process/settings_processing.cs"), item => item.Code == "UPD102");
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
