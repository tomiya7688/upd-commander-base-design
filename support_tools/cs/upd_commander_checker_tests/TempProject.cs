using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

internal sealed class TempProject : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        "upd-checker-tests",
        Guid.NewGuid().ToString("N")
    );

    internal TempProject()
    {
        Directory.CreateDirectory(root);
    }

    internal string PathFor(string relative)
    {
        return Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
    }

    internal string CreateDirectory(string relative)
    {
        var path = PathFor(relative);
        Directory.CreateDirectory(path);
        return path;
    }

    internal void Write(string relative, string content)
    {
        var path = PathFor(relative);
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }
        File.WriteAllText(path, content);
    }

    internal List<Finding> Scan(IReadOnlyList<string>? ignore = null, int upd301MaxInputs = 2)
    {
        return Scanner.ScanPath(new ScanPathInput(root, ignore ?? [], upd301MaxInputs));
    }

    internal List<Finding> Scan(
        (int FlatLayerMinFiles, int FlatLayerMinDirectPercent) flatLayerThresholds
    )
    {
        return Scanner.ScanPath(
            new ScanPathInput(
                root,
                [],
                2,
                flatLayerThresholds.FlatLayerMinFiles,
                flatLayerThresholds.FlatLayerMinDirectPercent
            )
        );
    }

    internal List<Finding> ScanModel((int MinItems, int MinOccurrences) modelThresholds)
    {
        return Scanner.ScanPath(
            new ScanPathInput(
                root,
                [],
                2,
                12,
                80,
                modelThresholds.MinItems,
                modelThresholds.MinOccurrences
            )
        );
    }

    public void Dispose()
    {
        Directory.Delete(root, recursive: true);
    }
}
