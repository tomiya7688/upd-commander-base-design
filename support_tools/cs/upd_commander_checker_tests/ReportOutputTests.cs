using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class ReportOutputTests
{
    [Fact]
    public void ParentCreationFailureReturnsToolError()
    {
        var root = Directory.CreateTempSubdirectory("upd-output-");
        try
        {
            var blocker = Path.Combine(root.FullName, "blocker");
            File.WriteAllText(blocker, "file");
            var output = Path.Combine(blocker, "report.txt");

            var code = ReportOutput.Finish(new FinishInput(new[] { "OK" }, output, 0));

            Assert.Equal(2, code);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void DirectoryOutputFailureReturnsToolError()
    {
        var root = Directory.CreateTempSubdirectory("upd-output-");
        try
        {
            var output = Path.Combine(root.FullName, "report");
            Directory.CreateDirectory(output);

            var code = ReportOutput.Finish(
                new FinishInput(new[] { "FAIL e=1 w=0 a=0" }, output, 1)
            );

            Assert.Equal(2, code);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }
}
