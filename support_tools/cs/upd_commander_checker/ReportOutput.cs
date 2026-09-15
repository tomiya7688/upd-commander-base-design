namespace UpdCommanderChecker;

internal static class ReportOutput
{
    internal static int Finish(FinishInput input)
    {
        var values = input.Lines.ToList();
        foreach (var line in values)
        {
            Console.WriteLine(line);
        }
        if (string.IsNullOrWhiteSpace(input.Output))
        {
            return input.ExitCode;
        }

        try
        {
            var parent = Path.GetDirectoryName(input.Output);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }
            File.WriteAllLines(input.Output, values);
        }
        catch (Exception exception)
            when (exception
                    is IOException
                        or UnauthorizedAccessException
                        or ArgumentException
                        or NotSupportedException
            )
        {
            Console.WriteLine($"I/O ERROR: failed to write output: {input.Output}");
            return 2;
        }
        return input.ExitCode;
    }
}
