namespace UpdCommanderChecker;

internal sealed record FindNameInput(IEnumerable<string> Parts, HashSet<string> Candidates);
