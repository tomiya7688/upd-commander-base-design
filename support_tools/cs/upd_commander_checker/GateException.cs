using System.Text.Json.Serialization;

namespace UpdCommanderChecker;

internal sealed record GateException(
    [property: JsonPropertyName("rule")] string Rule,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("line")] int? Line
);
