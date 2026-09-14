using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class CliIgnoreTests
{
    [Fact]
    public void CliPathIgnoreSkipsFile()
    {
        using var project = new TempProject();
        project.Write("process/path_commander.cs", "namespace Sample; internal sealed class PathCommander { internal int Run() => 1 + 2; }");
        Assert.DoesNotContain(project.Scan(["process/**"]), item => item.Code == "UPD202");
    }
}
