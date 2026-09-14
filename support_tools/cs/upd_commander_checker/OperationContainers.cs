namespace UpdCommanderChecker;

internal sealed record ScanPathInput(
    string Target,
    IReadOnlyList<string> CliIgnore);

internal sealed record ScanFileInput(
    string File,
    string Relative,
    IReadOnlyList<IgnoreRule> IgnoreRules);

internal sealed record DependencyCheckInput(
    ModuleInfo Source,
    ModuleInfo Target);

internal sealed record IgnoreCheckInput(
    string Path,
    string Code,
    string LineText,
    IReadOnlyList<IgnoreRule> Rules);

internal sealed record GlobMatchInput(
    string Path,
    string Pattern);

internal sealed record AddFindingInput(
    List<Finding> Findings,
    string Path,
    int Line,
    string Code,
    string Message,
    string Severity,
    string LineText,
    IReadOnlyList<IgnoreRule> IgnoreRules);

internal sealed record FinishInput(
    IEnumerable<string> Lines,
    string Output,
    int ExitCode);

internal sealed record FindNameInput(
    IEnumerable<string> Parts,
    HashSet<string> Candidates);

internal sealed record FindPathRoleInput(
    IEnumerable<string> Directories,
    string Stem);

internal sealed record ResolvePathInput(
    string Root,
    string Value);
