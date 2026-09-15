using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal sealed record ParsedSource(
    string File,
    string Relative,
    SyntaxTree Tree,
    CompilationUnitSyntax Root,
    IReadOnlyList<string> Lines
);
