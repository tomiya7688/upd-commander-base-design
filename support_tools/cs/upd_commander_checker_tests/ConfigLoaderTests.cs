using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class ConfigLoaderTests
{
    [Theory]
    [InlineData("{\"input\":null}", "input")]
    [InlineData("{\"output\":null}", "output")]
    [InlineData("{\"ignore\":null}", "ignore")]
    [InlineData("{\"ignore\":[null]}", "ignore")]
    [InlineData("{\"warnings_as_errors\":null}", "warnings_as_errors")]
    [InlineData("{\"fail_on\":null}", "fail_on")]
    [InlineData("{\"fail_on\":[\"fatal\"]}", "fail_on")]
    [InlineData("{\"severity_overrides\":null}", "severity_overrides")]
    [InlineData("{\"severity_overrides\":[]}", "severity_overrides")]
    [InlineData("{\"severity_overrides\":{\"UPD101\":null}}", "severity_overrides")]
    [InlineData("{\"severity_overrides\":{\"UPD101\":\"fatal\"}}", "severity_overrides")]
    [InlineData("{\"severity_overrides\":{\"bad\":\"error\"}}", "severity_overrides")]
    [InlineData("{\"gate_exceptions\":null}", "gate_exceptions")]
    [InlineData(
        "{\"gate_exceptions\":[{\"rule\":\"UPD203\",\"path\":\"../a.cs\",\"reason\":\"bad\"}]}",
        "gate_exceptions"
    )]
    [InlineData(
        "{\"gate_exceptions\":[{\"rule\":\"UPD203\",\"path\":\"a.cs\",\"reason\":\"ok\",\"extra\":1}]}",
        "gate_exceptions"
    )]
    [InlineData("{\"enabled_rules\":null}", "enabled_rules")]
    [InlineData("{\"input\":1}", "input")]
    [InlineData("{\"warnings_as_errors\":\"true\"}", "warnings_as_errors")]
    [InlineData("{\"common_roots\":null}", "common_roots")]
    [InlineData("{\"common_roots\":\"common\"}", "common_roots")]
    [InlineData("{\"common_roots\":[null]}", "common_roots")]
    [InlineData("{\"common_roots\":[\"\"]}", "common_roots")]
    [InlineData("{\"common_roots\":[\".\"]}", "common_roots")]
    [InlineData("{\"common_roots\":[\"..\"]}", "common_roots")]
    [InlineData("{\"common_roots\":[\"ui\"]}", "common_roots")]
    [InlineData("{\"common_roots\":[\"process\"]}", "common_roots")]
    [InlineData("{\"common_roots\":[\"data\"]}", "common_roots")]
    [InlineData("{\"common_roots\":[\"nested/common\"]}", "common_roots")]
    [InlineData("{\"upd301_max_inputs\":null}", "upd301_max_inputs")]
    [InlineData("{\"upd301_max_inputs\":true}", "upd301_max_inputs")]
    [InlineData("{\"upd301_max_inputs\":\"2\"}", "upd301_max_inputs")]
    [InlineData("{\"upd301_max_inputs\":2.0}", "upd301_max_inputs")]
    [InlineData("{\"upd301_max_inputs\":0}", "upd301_max_inputs")]
    [InlineData("{\"upd301_max_inputs\":-1}", "upd301_max_inputs")]
    [InlineData("{\"flat_layer_min_files\":null}", "flat_layer_min_files")]
    [InlineData("{\"flat_layer_min_files\":true}", "flat_layer_min_files")]
    [InlineData("{\"flat_layer_min_files\":0}", "flat_layer_min_files")]
    [InlineData("{\"flat_layer_min_direct_percent\":null}", "flat_layer_min_direct_percent")]
    [InlineData("{\"flat_layer_min_direct_percent\":0}", "flat_layer_min_direct_percent")]
    [InlineData("{\"flat_layer_min_direct_percent\":101}", "flat_layer_min_direct_percent")]
    [InlineData("{\"flat_layer_min_direct_percent\":80.0}", "flat_layer_min_direct_percent")]
    [InlineData("{\"model_group_min_items\":null}", "model_group_min_items")]
    [InlineData("{\"model_group_min_items\":2}", "model_group_min_items")]
    [InlineData("{\"model_group_min_items\":3.0}", "model_group_min_items")]
    [InlineData("{\"model_group_min_occurrences\":null}", "model_group_min_occurrences")]
    [InlineData("{\"model_group_min_occurrences\":1}", "model_group_min_occurrences")]
    [InlineData("{\"model_group_min_occurrences\":2.0}", "model_group_min_occurrences")]
    public void InvalidTypedFieldsAreRejected(string content, string field)
    {
        var path = WriteConfig(content);

        var exception = Assert.Throws<ConfigException>(() => ConfigLoader.LoadFromPath(path));

        Assert.Contains($"invalid config field: {field}", exception.Message);
    }

    [Fact]
    public void CommonRootsDefaultAndLoad()
    {
        var defaults = ConfigLoader.LoadFromPath(WriteConfig("{}"));
        Assert.Equal(["common", "shared"], defaults.CommonRoots);

        var configured = ConfigLoader.LoadFromPath(
            WriteConfig("{\"common_roots\":[\"contracts\",\"Shared\",\"contracts\"]}")
        );
        Assert.Equal(["contracts", "shared"], configured.CommonRoots);

        var disabled = ConfigLoader.LoadFromPath(WriteConfig("{\"common_roots\":[]}"));
        Assert.Empty(disabled.CommonRoots);
    }

    [Fact]
    public void Upd301MaxInputsDefaultsToTwo()
    {
        var path = WriteConfig("{}");

        var config = ConfigLoader.LoadFromPath(path);

        Assert.Equal(2, config.Upd301MaxInputs);
    }

    [Fact]
    public void Upd301MaxInputsLoadsPositiveInteger()
    {
        var path = WriteConfig("{\"upd301_max_inputs\":3}");

        var config = ConfigLoader.LoadFromPath(path);

        Assert.Equal(3, config.Upd301MaxInputs);
    }

    [Fact]
    public void FlatLayerThresholdsDefaultAndLoad()
    {
        var defaults = ConfigLoader.LoadFromPath(WriteConfig("{}"));
        Assert.Equal(12, defaults.FlatLayerMinFiles);
        Assert.Equal(80, defaults.FlatLayerMinDirectPercent);

        var configured = ConfigLoader.LoadFromPath(
            WriteConfig("{\"flat_layer_min_files\":14,\"flat_layer_min_direct_percent\":90}")
        );
        Assert.Equal(14, configured.FlatLayerMinFiles);
        Assert.Equal(90, configured.FlatLayerMinDirectPercent);
    }

    [Fact]
    public void ModelThresholdsDefaultAndLoad()
    {
        var defaults = ConfigLoader.LoadFromPath(WriteConfig("{}"));
        Assert.Equal(3, defaults.ModelGroupMinItems);
        Assert.Equal(2, defaults.ModelGroupMinOccurrences);

        var configured = ConfigLoader.LoadFromPath(
            WriteConfig("{\"model_group_min_items\":4,\"model_group_min_occurrences\":3}")
        );
        Assert.Equal(4, configured.ModelGroupMinItems);
        Assert.Equal(3, configured.ModelGroupMinOccurrences);
    }

    [Fact]
    public void GatePolicyLoadsAndNormalizes()
    {
        var config = ConfigLoader.LoadFromPath(
            WriteConfig(
                "{\"fail_on\":[\" ERROR \",\"warning\",\"error\"],"
                    + "\"severity_overrides\":{\"upd203\":\"WARNING\"},"
                    + "\"gate_exceptions\":[{\"rule\":\"upd203\",\"path\":\"src/a.cs\",\"line\":8,\"reason\":\"approved\"}]}"
            )
        );

        Assert.Equal(["error", "warning"], config.FailOn);
        Assert.Equal("warning", config.SeverityOverrides["UPD203"]);
        Assert.Equal("UPD203", Assert.Single(config.GateExceptions).Rule);
        Assert.Equal(8, config.GateExceptions[0].Line);
    }

    [Fact]
    public void ExplicitEmptyGateIsNotTreatedAsMissing()
    {
        var config = ConfigLoader.LoadFromPath(WriteConfig("{\"fail_on\":[]}"));

        Assert.NotNull(config.FailOn);
        Assert.Empty(config.FailOn);
    }

    [Fact]
    public void NullRootIsRejected()
    {
        var path = WriteConfig("null");

        var exception = Assert.Throws<ConfigException>(() => ConfigLoader.LoadFromPath(path));

        Assert.Contains("invalid config:", exception.Message);
    }

    private static string WriteConfig(string content)
    {
        var root = Path.Combine(Path.GetTempPath(), $"upd-cs-config-{Guid.NewGuid():N}");
        var configDirectory = Path.Combine(root, "config");
        Directory.CreateDirectory(configDirectory);
        var path = Path.Combine(configDirectory, "path.json");
        File.WriteAllText(path, content);
        return path;
    }
}
