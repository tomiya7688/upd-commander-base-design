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
		if message := DependencyError(source, target); message != "" {
			code := "UPD101"
			if message == "cross-application internal dependency" {
				code = "UPD102"
			}
			addFinding(&findings, rel, line, code, message, "error", lines, rules)
			continue
		}
		if warning := DataCommanderWarning(source, target); warning != "" {
			addFinding(&findings, rel, line, "UPD103", warning, "warning", lines, rules)
		}
	}
	return findings
}
