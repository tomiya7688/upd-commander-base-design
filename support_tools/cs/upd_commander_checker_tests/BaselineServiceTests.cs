using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class BaselineServiceTests
{
    [Theory]
    [InlineData(
        "UPD101",
        "src/ui/screen.cs",
        "Ui.Screen.Run",
        "target=data.storage",
        "sha256:ea890f274ed082af81eded22f8f1f869348dc62665221bff76200c3543e86cd1"
    )]
    [InlineData(
        "UPD101",
        "src/cafe\u0301/screen.cs",
        "Ui.Screen.Run",
        "target=data.storage",
        "sha256:467a3d78fc930c5fd23a168378526543322a1ad98fc1e66150d1a504eea8783a"
    )]
    public void FingerprintMatchesSharedVectors(
        string rule,
        string path,
        string symbol,
        string context,
        string expected
    )
    {
        Assert.Equal(expected, BaselineService.Fingerprint(rule, path, symbol, context));
    }

    [Fact]
    public void WriteLoadAndCompareClassifiesAllStates()
    {
        var old = new Finding(
            "src/a.cs",
            4,
            "UPD101",
            "old message",
            "error",
            "Ui.Screen.Run",
            "target=Data.Save"
        );
        var added = new Finding(
            "src/b.cs",
            7,
            "UPD203",
            "new message",
            "warning",
            "Process.Run",
            "syntax:File.Read"
        );
        var directory = Path.Combine(
            Path.GetTempPath(),
            "upd-baseline-" + Guid.NewGuid().ToString("N")
        );
        var file = Path.Combine(directory, "baseline.json");
        try
        {
            BaselineService.Write(file, [old]);
            var loaded = BaselineService.Load(file);
            var comparison = BaselineService.Compare(
                [old with { Line = 99, Message = "changed" }, added],
                loaded
            );
            Assert.Single(comparison.Existing);
            Assert.Single(comparison.New);
            Assert.Empty(comparison.Resolved);
            var resolved = BaselineService.Compare([], loaded);
            Assert.Single(resolved.Resolved);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void InvalidVersionAndCorruptJsonFailClearly()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "upd-baseline-" + Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, "baseline.json");
        try
        {
            File.WriteAllText(
                file,
                "{\"schema_version\":2,\"fingerprint_version\":1,\"findings\":[]}"
            );
            Assert.Contains(
                "unsupported schema_version",
                Assert.Throws<InvalidDataException>(() => BaselineService.Load(file)).Message
            );
            File.WriteAllText(file, "{ broken");
            Assert.ThrowsAny<System.Text.Json.JsonException>(() => BaselineService.Load(file));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
