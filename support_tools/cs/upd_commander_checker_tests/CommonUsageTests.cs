namespace UpdCommanderChecker.Tests;

public sealed class CommonUsageTests
{
    [Fact]
    public void OneReferringLayerProducesWarning()
    {
        using var project = CreateProject("ui");

        var finding = Assert.Single(project.Scan(), item => item.Code == "UPD407");

        Assert.Equal("warning", finding.Severity);
        Assert.Equal("common/contracts/Message.cs", finding.Path);
    }

    [Theory]
    [InlineData("ui", "process")]
    [InlineData("ui", "process", "data")]
    public void TwoOrMoreReferringLayersDoNotProduceWarning(params string[] layers)
    {
        using var project = CreateProject(layers);

        Assert.DoesNotContain(project.Scan(), item => item.Code == "UPD407");
    }

    [Fact]
    public void UnusedPublicCommonTypeIsNotReported()
    {
        using var project = new TempProject();
        project.Write("common/contracts/Message.cs", "namespace Common.Contracts; public sealed class Message {}\n");

        Assert.DoesNotContain(project.Scan(), item => item.Code == "UPD407");
    }

    [Theory]
    [InlineData("ui/tests/Consumer.cs")]
    [InlineData("ui/generated/Consumer.cs")]
    public void TestAndGeneratedReferencesDoNotCount(string consumerPath)
    {
        using var project = new TempProject();
        WriteCommon(project);
        WriteConsumer(project, consumerPath);

        Assert.DoesNotContain(project.Scan(), item => item.Code == "UPD407");
    }

    [Fact]
    public void RuleIgnoreAndCustomCommonRootHaveConsistentMeaning()
    {
        using var project = new TempProject();
        project.Write("contracts/Message.cs", "namespace Common.Contracts; public sealed class Message {}\n");
        project.Write("ui/Consumer.cs", "using Common.Contracts; namespace UI; public sealed class Consumer { public Message Get() => new(); }\n");

        Assert.Contains(project.Scan(commonRoots: ["contracts"]), item => item.Code == "UPD407");
        project.Write(".updcommanderignore", "UPD407 contracts/**\n");
        Assert.DoesNotContain(project.Scan(commonRoots: ["contracts"]), item => item.Code == "UPD407");
    }

    private static TempProject CreateProject(params string[] layers)
    {
        var project = new TempProject();
        WriteCommon(project);
        foreach (var layer in layers)
        {
            WriteConsumer(project, $"{layer}/Consumer.cs");
        }
        return project;
    }

    private static void WriteCommon(TempProject project)
    {
        project.Write("common/contracts/Message.cs", "namespace Common.Contracts; public sealed class Message {}\n");
    }

    private static void WriteConsumer(TempProject project, string path)
    {
        project.Write(path, "using Common.Contracts; namespace Consumer; public sealed class View { public Message Get() => new(); }\n");
    }
}
