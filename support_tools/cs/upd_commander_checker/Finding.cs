namespace UpdCommanderChecker;

internal sealed record Finding(string Path, int Line, string Code, string Message, string Severity = "error");
