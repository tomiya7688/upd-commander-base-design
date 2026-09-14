using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class InlineIgnoreTests
{
    [Fact]
    public void InlineIgnoreSuppressesRule()
    {
        using var project = new TempProject();
        project.Write(
            "process/inline_commander.cs",
            "namespace Sample; internal sealed class InlineCommander { internal int Run() => 1 + 2; // upd: ignore UPD202 - test\n}"
        );
        Assert.DoesNotContain(project.Scan(), item => item.Code == "UPD202");
    }
}
