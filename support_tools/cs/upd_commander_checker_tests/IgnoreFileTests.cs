using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class IgnoreFileTests
{
    [Fact]
    public void IgnoreFileSuppressesSpecificRule()
    {
        using var project = new TempProject();
        project.Write(
            "process/rule_commander.cs",
            "namespace Sample; internal sealed class RuleCommander { internal int Run() => 1 + 2; }"
        );
        project.Write(".updcommanderignore", "UPD202 process/rule_commander.cs # test\n");
        Assert.DoesNotContain(project.Scan(), item => item.Code == "UPD202");
    }
}
