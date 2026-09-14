using System.Text.Json.Serialization;

namespace UpdCommanderChecker;

internal sealed class CheckerConfig
{
    [JsonPropertyName("input")]
    public string Input { get; set; } = ".";

    [JsonPropertyName("output")]
    public string Output { get; set; } = "";

    [JsonPropertyName("ignore")]
    public List<string> Ignore { get; set; } = new();

    [JsonPropertyName("warnings_as_errors")]
    public bool WarningsAsErrors { get; set; }

    [JsonPropertyName("enabled_rules")]
    public List<string>? EnabledRules { get; set; }
}
