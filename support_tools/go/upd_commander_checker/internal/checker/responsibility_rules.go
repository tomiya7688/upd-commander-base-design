package checker

import (
	"go/ast"
	"go/token"
	"strings"
)

const maxResponsibilityLines = 250
const maxResponsibilityMethods = 12

func checkResponsibilities(file *ast.File, fset *token.FileSet, path string, lines []string, rules []IgnoreRule) []Finding {
	var findings []Finding

	if nonBlankLineCount(lines) > maxResponsibilityLines {
		addFinding(&findings, path, 1, "UPD401", "file is too large for one responsibility", "warning", lines, rules)
	}

	methodCounts := map[string]int{}
	methodLines := map[string]int{}
	for _, declaration := range file.Decls {
		function, ok := declaration.(*ast.FuncDecl)
		if !ok || function.Recv == nil || len(function.Recv.List) == 0 {
			continue
		}
		receiver := receiverName(function.Recv.List[0].Type)
		if receiver == "" {
			continue
		}
		methodCounts[receiver]++
		if methodLines[receiver] == 0 {
			methodLines[receiver] = fset.Position(function.Pos()).Line
		}
	}

	for receiver, count := range methodCounts {
		if count > maxResponsibilityMethods {
			addFinding(&findings, path, methodLines[receiver], "UPD401", "type "+receiver+" has too many methods for one responsibility", "warning", lines, rules)
		}
	}
	return findings
}

func receiverName(expression ast.Expr) string {
	switch value := expression.(type) {
	case *ast.Ident:
		return value.Name
	case *ast.StarExpr:
		return receiverName(value.X)
	default:
		return ""
	}
}

func nonBlankLineCount(lines []string) int {
	count := 0
	for _, line := range lines {
		if strings.TrimSpace(line) != "" {
			count++
		}
	}
	return count
}
