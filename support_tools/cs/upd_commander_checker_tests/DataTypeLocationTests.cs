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
}
