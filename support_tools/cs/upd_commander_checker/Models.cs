namespace UpdCommanderChecker;

internal sealed record Finding(string Path, int Line, string Code, string Message, string Severity = "error");

internal sealed record ModuleInfo(string Path, string Layer, string Role, string ApplicationId);
