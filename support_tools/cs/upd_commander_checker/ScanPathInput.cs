namespace UpdCommanderChecker;

internal sealed record ScanPathInput(
    string Target,
    IReadOnlyList<string> CliIgnore,
    int Upd301MaxInputs = 2,
    int FlatLayerMinFiles = 12,
    int FlatLayerMinDirectPercent = 80
);
