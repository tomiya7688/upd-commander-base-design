namespace UpdCommanderChecker;

internal sealed record ModelGroupOccurrence(
    string Path,
    int Line,
    string Application,
    string Layer,
    string Kind,
    IReadOnlyList<string> Items
)
{
    internal string Signature =>
        string.Join("\u001f", Application, Layer, Kind, string.Join("\u001e", Items));
}
