namespace UpdCommanderChecker;

internal sealed record PathIgnoreCheckInput(string Path, IReadOnlyList<IgnoreRule> Rules);
