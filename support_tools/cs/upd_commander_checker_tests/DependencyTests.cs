using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class DependencyTests
{
    [Fact]
    public void DependencyRulesHaveExpectedCodes()
    {
        using var project = new TempProject();
        project.Write("ui/screen_processing.cs", "using Data.Storage;\nnamespace Sample;\n");
        project.Write("applications/main/process/main_commander.cs", "using Applications.Settings.Process.SettingsProcessing;\nnamespace Sample;\n");
        project.Write("data/save_commander.cs", "using Data.CacheCommander;\nnamespace Sample;\n");
        var findings = project.Scan();
        TestAssert.Has(findings, "UPD101", "error");
        TestAssert.Has(findings, "UPD102", "error");
        TestAssert.Has(findings, "UPD103", "warning");
    }
}
