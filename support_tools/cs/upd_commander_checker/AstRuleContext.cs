namespace UpdCommanderChecker;

internal sealed record AstRuleContext(
    List<Finding> Findings,
    AstAnalysisInput Analysis);
