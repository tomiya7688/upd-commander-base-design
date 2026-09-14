namespace UpdCommanderChecker;

internal sealed record FinishInput(IEnumerable<string> Lines, string Output, int ExitCode);
