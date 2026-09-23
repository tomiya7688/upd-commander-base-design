namespace UpdCommanderChecker;

internal sealed record ScanPathInput(
    string Target,
    IReadOnlyList<string> CliIgnore,
    int Upd301MaxInputs = 2,
    int FlatLayerMinFiles = 12,
    int FlatLayerMinDirectPercent = 80,
    int ModelGroupMinItems = 3,
    int ModelGroupMinOccurrences = 2,
    IReadOnlyList<string>? CommonRoots = null
);
