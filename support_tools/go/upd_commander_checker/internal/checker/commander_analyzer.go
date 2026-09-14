package checker

import (
	"go/ast"
	"go/token"
)

func checkCommander(file *ast.File, fset *token.FileSet, source ModuleInfo, rel string, lines []string, rules []IgnoreRule) []Finding {
	var findings []Finding
	if source.Role != "commander" {
		return findings
	}

	ast.Inspect(file, func(node ast.Node) bool {
		switch value := node.(type) {
		case *ast.ForStmt, *ast.RangeStmt:
			addFinding(&findings, rel, fset.Position(node.Pos()).Line, "UPD201", "Commander loop", "warning", lines, rules)
		case *ast.BinaryExpr:
			addFinding(&findings, rel, fset.Position(node.Pos()).Line, "UPD202", "Commander calculation", "warning", lines, rules)
		case *ast.CallExpr:
			if isDirectWorkCall(value) {
				addFinding(&findings, rel, fset.Position(node.Pos()).Line, "UPD203", "Commander direct I/O/API call", "error", lines, rules)
			}
		}
		return true
	})
	return findings
}

func isDirectWorkCall(call *ast.CallExpr) bool {
	selector, ok := call.Fun.(*ast.SelectorExpr)
	if !ok {
		return false
	}
	ident, ok := selector.X.(*ast.Ident)
	if !ok {
		return false
	}
	forbidden := map[string]bool{"os": true, "ioutil": true, "json": true, "sql": true, "http": true}
	return forbidden[ident.Name]
}
