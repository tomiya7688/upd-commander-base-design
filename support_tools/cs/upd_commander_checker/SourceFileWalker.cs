namespace UpdCommanderChecker;

internal static class SourceFileWalker
{
    internal static List<string> Enumerate(SourceFileWalkInput input)
    {
        var files = new List<string>();
        var fullTarget = Path.GetFullPath(input.Target);
        if (File.Exists(fullTarget))
        {
            if (
                string.Equals(
                    Path.GetExtension(fullTarget),
                    ".cs",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                files.Add(fullTarget);
            }
            return files;
        }
        if (!Directory.Exists(fullTarget))
        {
            input.Findings.Add(
                new Finding(
                    Path.GetRelativePath(input.Root, fullTarget).Replace('\\', '/'),
                    1,
                    "UPD001",
                    "read failed: target does not exist"
                )
            );
            return files;
        }

        var pending = new Stack<string>();
        pending.Push(fullTarget);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            string[] entries;
            try
            {
                entries = Directory.GetFileSystemEntries(directory);
            }
            catch (Exception exception)
                when (exception
                        is IOException
                            or UnauthorizedAccessException
                            or System.Security.SecurityException
                )
            {
                input.Findings.Add(
                    new Finding(
                        Path.GetRelativePath(input.Root, directory).Replace('\\', '/'),
                        1,
                        "UPD001",
                        $"read failed: {exception.Message}"
                    )
                );
                continue;
            }

            foreach (var entry in entries)
            {
                try
                {
                    var attributes = File.GetAttributes(entry);
                    if ((attributes & FileAttributes.Directory) != 0)
                    {
                        if ((attributes & FileAttributes.ReparsePoint) == 0)
                        {
                            pending.Push(entry);
                        }
                        continue;
                    }
                    if (
                        string.Equals(
                            Path.GetExtension(entry),
                            ".cs",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        files.Add(entry);
                    }
                }
                catch (Exception exception)
                    when (exception
                            is IOException
                                or UnauthorizedAccessException
                                or System.Security.SecurityException
                    )
                {
                    input.Findings.Add(
                        new Finding(
                            Path.GetRelativePath(input.Root, entry).Replace('\\', '/'),
                            1,
                            "UPD001",
                            $"read failed: {exception.Message}"
                        )
                    );
                }
            }
        }
        return files;
    }
}
