using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class CommonClassifierTests
{
    [Fact]
    public void DefaultsClassifyCommonAndSharedAsNeutral()
    {
        var common = Classifier.ClassifyPath("applications/main/common/contracts/message.cs");
        var shared = Classifier.ClassifyReference("applications.main.shared.contracts.message");

        Assert.Equal("main", common.ApplicationId);
        Assert.Equal("common", common.Layer);
        Assert.Equal("main", shared.ApplicationId);
        Assert.Equal("common", shared.Layer);
    }

    [Fact]
    public void CustomAndDisabledRootsAreSupported()
    {
        var custom = Classifier.ClassifyPath(
            "applications/main/contracts/message.cs",
            ["contracts"]
        );
        var disabled = Classifier.ClassifyPath("common/message.cs", []);

        Assert.Equal("common", custom.Layer);
        Assert.Equal(string.Empty, disabled.Layer);
    }

    [Fact]
    public void InnermostLayerOrCommonMarkerWins()
    {
        Assert.Equal("ui", Classifier.ClassifyPath("common/ui/screen.cs").Layer);
        Assert.Equal("common", Classifier.ClassifyPath("ui/common/message.cs").Layer);
    }
}
