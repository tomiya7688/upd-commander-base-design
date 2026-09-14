using Microsoft.CodeAnalysis;

namespace UpdCommanderChecker;

internal sealed record NodeFindingInput(
    AstRuleContext Context,
    SyntaxNode Node,
    string Code,
    string Message,
    string Severity
);
