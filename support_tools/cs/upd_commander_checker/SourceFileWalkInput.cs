namespace UpdCommanderChecker;

internal sealed record SourceFileWalkInput(string Target, string Root, List<Finding> Findings);
