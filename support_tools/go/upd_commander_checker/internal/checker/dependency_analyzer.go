package checker

import (
	"go/ast"
	"go/token"
	"strings"
)

func checkDependencies(file *ast.File, fset *token.FileSet, source ModuleInfo, rel string, lines []string, rules []IgnoreRule) []Finding {
	var findings []Finding
	for _, spec := range file.Imports {
		name := strings.Trim(spec.Path.Value, "\"")
		target := ClassifyImport(name)
		line := fset.Position(spec.Pos()).Line
		if result := DependencyResult(source, target); result != nil {
			addFinding(&findings, rel, line, result.Code, result.Message, result.Severity, lines, rules)
		}
	}
	return findings
}
