using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class DataTypeLocationTests
{
    [Fact]
    public void ColocatedDataTypeIsAttention()
    {
        using var project = new TempProject();
        project.Write(
            "models.cs",
            "namespace Sample; internal sealed class First { internal int Value { get; init; } } internal sealed class Second { internal int Value { get; init; } }"
        );
        TestAssert.Has(project.Scan(), "UPD403", "attention");
    }

    [Fact]
    public void ExternalUsePromotesColocatedDataTypeToWarning()
    {
        using var project = new TempProject();
        project.Write(
            "models.cs",
            "namespace Sample; internal sealed class First { internal int Value { get; init; } } internal sealed class Second { internal int Value { get; init; } }"
        );
        project.Write(
            "consumer.cs",
            "namespace Sample; internal sealed class Consumer { private First? value; }"
        );
        TestAssert.Has(project.Scan(), "UPD404", "warning");
    }

    [Fact]
    public void SameNameLocalVariableDoesNotPromoteType()
    {
        using var project = new TempProject();
        project.Write(
            "models.cs",
            "namespace Sample; internal sealed class First { internal int Value { get; init; } } internal sealed class Second { internal int Value { get; init; } }"
        );
        project.Write(
            "consumer.cs",
            "namespace Sample; internal sealed class Consumer { internal int Read() { var First = 1; return First; } }"
        );
        var findings = project.Scan();
        Assert.DoesNotContain(
            findings,
            finding =>
                finding.Path == "models.cs"
                && finding.Code == "UPD404"
                && finding.Message.Contains("First")
        );
    }

    [Fact]
    public void SameNameTypeInOtherNamespaceDoesNotPromoteCandidate()
    {
        using var project = new TempProject();
        project.Write(
            "models.cs",
            "namespace Models; internal sealed class First { internal int Value { get; init; } } internal sealed class Second { internal int Value { get; init; } }"
        );
        project.Write(
            "consumer.cs",
            "namespace Other; internal sealed class First { } internal sealed class Consumer { private First? value; }"
        );
        var findings = project.Scan();
        Assert.Contains(
            findings,
            finding =>
                finding.Path == "models.cs"
                && finding.Code == "UPD403"
                && finding.Message.Contains("First")
        );
        Assert.DoesNotContain(
            findings,
            finding =>
                finding.Path == "models.cs"
                && finding.Code == "UPD404"
                && finding.Message.Contains("First")
        );
    }

    [Fact]
    public void OnlyReferencedSiblingTypeIsPromoted()
    {
        using var project = new TempProject();
        project.Write(
            "models.cs",
            "namespace Sample; internal sealed class First { internal int Value { get; init; } } internal sealed class Second { internal int Value { get; init; } }"
        );
        project.Write(
            "consumer.cs",
            "namespace Sample; internal sealed class Consumer { private Second? value; }"
        );
        var findings = project.Scan();
        Assert.Contains(
            findings,
            finding =>
                finding.Path == "models.cs"
                && finding.Code == "UPD403"
                && finding.Message.Contains("First")
        );
        Assert.Contains(
            findings,
            finding =>
                finding.Path == "models.cs"
                && finding.Code == "UPD404"
                && finding.Message.Contains("Second")
        );
    }
}
