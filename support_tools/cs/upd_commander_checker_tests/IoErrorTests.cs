namespace UpdCommanderChecker.Tests;

public sealed class IoErrorTests
{
    [Fact]
    public void MissingIgnoreFileIsNormal()
    {
        using var project = new TempProject();
        project.Write("plain.cs", "namespace Sample; internal sealed class Plain { }");

        Assert.DoesNotContain(project.Scan(), item => item.Code == "UPD001");
    }

    [Fact]
    public void UnreadableIgnorePathReportsUPD001AndStopsScan()
    {
        using var project = new TempProject();
        project.CreateDirectory(".updcommanderignore");
        project.Write("broken.cs", "namespace Sample; internal sealed class Broken {");

        var findings = project.Scan();

        Assert.Contains(findings, item => item.Code == "UPD001");
        Assert.DoesNotContain(findings, item => item.Code == "UPD002");
    }

    [Fact]
    public void UnreadableDirectoryReportsUPD001AndContinues()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using var project = new TempProject();
        var blocked = project.CreateDirectory("blocked");
        project.Write("blocked/hidden.cs", "namespace Sample; internal sealed class Hidden { }");
        project.Write(
            "process/loop_commander.cs",
            "namespace Sample; internal sealed class LoopCommander { internal void Run() { for (var i = 0; i < 3; i++) { } } }"
        );
        File.SetUnixFileMode(blocked, UnixFileMode.None);
        try
        {
            try
            {
                _ = Directory.GetFileSystemEntries(blocked);
            }
            catch (UnauthorizedAccessException)
            {
                var findings = project.Scan();
                Assert.Contains(findings, item => item.Code == "UPD001");
                Assert.Contains(findings, item => item.Code == "UPD201");
                return;
            }
            catch (IOException)
            {
                var findings = project.Scan();
                Assert.Contains(findings, item => item.Code == "UPD001");
                Assert.Contains(findings, item => item.Code == "UPD201");
                return;
            }
        }
        finally
        {
            File.SetUnixFileMode(
                blocked,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
            );
        }
    }
}
