using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

public sealed class InlineIgnoreTests
{
    [Fact]
    public void InlineIgnoreSuppressesRule()
    {
        using var project = new TempProject();
        project.Write(
            "process/inline_commander.cs",
            "namespace Sample; internal sealed class InlineCommander { internal int Run() => 1 + 2; // upd: ignore UPD202 - test\n}"
        );
        Assert.DoesNotContain(project.Scan(), item => item.Code == "UPD202");
    }

    [Fact]
    public void InlineIgnoreSuppressesUpd401()
    {
        using var project = new TempProject();
        var methods = string.Join("\n", Enumerable.Range(0, 13).Select(index => $"internal void M{index}() {{ }}"));
        project.Write(
            "large.cs",
            $"namespace Sample;\ninternal sealed class Large // upd: ignore UPD401 - intentional\n{{\n{methods}\n}}"
        );
        Assert.DoesNotContain(project.Scan(), item => item.Code == "UPD401");
    }

    [Fact]
    public void InlineIgnoreSuppressesUpd402()
    {
        using var project = new TempProject();
        project.Write(
            "multiple.cs",
            "namespace Sample;\ninternal sealed class First { internal void Run() { } }\ninternal sealed class Second // upd: ignore UPD402 - intentional\n{ internal void Run() { } }"
        );
        Assert.DoesNotContain(project.Scan(), item => item.Code == "UPD402");
    }

    [Fact]
    public void InlineIgnoreSuppressesUpd403()
    {
        using var project = new TempProject();
        project.Write(
            "models.cs",
            "namespace Sample;\ninternal sealed class First // upd: ignore UPD403 - intentional\n{ internal int Value { get; init; } }\ninternal sealed class Second { internal int Value { get; init; } }"
        );
        var findings = project.Scan();
        Assert.DoesNotContain(
            findings,
            item => item.Code == "UPD403" && item.Message.Contains("First")
        );
        Assert.Contains(
            findings,
            item => item.Code == "UPD403" && item.Message.Contains("Second")
        );
    }

    [Fact]
    public void InlineIgnoreSuppressesUpd404()
    {
        using var project = new TempProject();
        project.Write(
            "models.cs",
            "namespace Sample;\ninternal sealed class First // upd: ignore UPD404 - intentional\n{ internal int Value { get; init; } }\ninternal sealed class Second { internal int Value { get; init; } }"
        );
        project.Write(
            "consumer.cs",
            "namespace Sample; internal sealed class Consumer { private First? value; }"
        );
        Assert.DoesNotContain(
            project.Scan(),
            item => item.Code == "UPD404" && item.Message.Contains("First")
        );
    }

    [Fact]
    public void InlineIgnoreDoesNotSuppressDifferentRule()
    {
        using var project = new TempProject();
        var methods = string.Join("\n", Enumerable.Range(0, 13).Select(index => $"internal void M{index}() {{ }}"));
        project.Write(
            "large.cs",
            $"namespace Sample;\ninternal sealed class Large // upd: ignore UPD402 - different rule\n{{\n{methods}\n}}"
        );
        Assert.Contains(project.Scan(), item => item.Code == "UPD401");
    }
}
