using Microsoft.CodeAnalysis;

namespace UpdCommanderChecker;

internal sealed record DirectWorkCheckInput(SyntaxNode Node, SemanticModel SemanticModel);
