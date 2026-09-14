namespace UpdCommanderChecker;

internal static class AstFindingEmitter
{
    internal static void Add(NodeFindingInput input)
    {
        var line = input.Node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var lineText =
            line > 0 && line <= input.Context.Analysis.Lines.Count
                ? input.Context.Analysis.Lines[line - 1]
                : string.Empty;
        if (
            IgnoreRules.IsIgnored(
                new IgnoreCheckInput(
                    input.Context.Analysis.Relative,
                    input.Code,
                    lineText,
                    input.Context.Analysis.IgnoreRules
                )
            )
        )
        {
            return;
        }

        input.Context.Findings.Add(
            new Finding(
                input.Context.Analysis.Relative,
                line,
                input.Code,
                input.Message,
                input.Severity
            )
        );
    }
}
