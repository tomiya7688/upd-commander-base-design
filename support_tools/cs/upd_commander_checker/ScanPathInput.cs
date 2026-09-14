namespace UpdCommanderChecker;

internal sealed record ScanPathInput(string Target, IReadOnlyList<string> CliIgnore);
