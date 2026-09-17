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
    [InlineData("{\"enabled_rules\":null}", "enabled_rules")]
    [InlineData("{\"input\":1}", "input")]
    [InlineData("{\"warnings_as_errors\":\"true\"}", "warnings_as_errors")]
    public void InvalidTypedFieldsAreRejected(string content, string field)
    {
        var path = WriteConfig(content);

        var exception = Assert.Throws<ConfigException>(() => ConfigLoader.LoadFromPath(path));

        Assert.Contains($"invalid config field: {field}", exception.Message);
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
