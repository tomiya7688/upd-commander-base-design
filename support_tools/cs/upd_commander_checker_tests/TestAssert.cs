using UpdCommanderChecker;

namespace UpdCommanderChecker.Tests;

internal static class TestAssert
{
    internal static void Has(IReadOnlyCollection<Finding> findings, string code, string severity)
    {
        Assert.Contains(findings, item => item.Code == code && item.Severity == severity);
    }
}
