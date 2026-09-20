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

    [JsonPropertyName("common_roots")]
    public List<string> CommonRoots { get; set; } = ["common", "shared"];

    [JsonPropertyName("enabled_rules")]
    public List<string>? EnabledRules { get; set; }

    [JsonPropertyName("upd301_max_inputs")]
    public int Upd301MaxInputs { get; set; } = 2;

    [JsonPropertyName("flat_layer_min_files")]
    public int FlatLayerMinFiles { get; set; } = 12;

    [JsonPropertyName("flat_layer_min_direct_percent")]
    public int FlatLayerMinDirectPercent { get; set; } = 80;

    [JsonPropertyName("model_group_min_items")]
    public int ModelGroupMinItems { get; set; } = 3;

    [JsonPropertyName("model_group_min_occurrences")]
    public int ModelGroupMinOccurrences { get; set; } = 2;
}
