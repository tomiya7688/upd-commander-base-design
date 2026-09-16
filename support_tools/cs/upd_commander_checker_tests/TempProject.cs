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

    internal List<Finding> Scan(IReadOnlyList<string>? ignore = null)
    {
        return Scanner.ScanPath(new ScanPathInput(root, ignore ?? []));
    }

    public void Dispose()
    {
        Directory.Delete(root, recursive: true);
    }
}
