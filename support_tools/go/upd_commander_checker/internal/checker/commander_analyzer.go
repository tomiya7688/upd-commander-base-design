package checker

import (
	"go/ast"
	"go/token"
	"path"
	"strconv"
)

func checkCommander(file *ast.File, fset *token.FileSet, source ModuleInfo, rel string, lines []string, rules []IgnoreRule) []Finding {
	var findings []Finding
	if source.Role != "commander" {
		return findings
	}

	imports := importAliases(file)
	ast.Inspect(file, func(node ast.Node) bool {
		switch value := node.(type) {
		case *ast.ForStmt, *ast.RangeStmt:
			addFinding(&findings, rel, fset.Position(node.Pos()).Line, "UPD201", "Commander loop", "warning", lines, rules)
		case *ast.BinaryExpr:
			if isArithmeticBinary(value.Op) {
				addFinding(&findings, rel, fset.Position(node.Pos()).Line, "UPD202", "Commander calculation", "warning", lines, rules)
			}
		case *ast.CallExpr:
			if isDirectWorkCall(value, imports) {
				addFinding(&findings, rel, fset.Position(node.Pos()).Line, "UPD203", "Commander direct I/O/API call", "error", lines, rules)
			}
		}
		return true
	})
	return findings
}

func isArithmeticBinary(operator token.Token) bool {
	switch operator {
	case token.ADD, token.SUB, token.MUL, token.QUO, token.REM:
		return true
	default:
		return false
	}
}

func importAliases(file *ast.File) map[string]string {
	aliases := make(map[string]string)
	for _, imported := range file.Imports {
		importPath, err := strconv.Unquote(imported.Path.Value)
		if err != nil || importPath == "" {
			continue
		}
		name := path.Base(importPath)
		if imported.Name != nil {
			if imported.Name.Name == "_" || imported.Name.Name == "." {
				continue
			}
			name = imported.Name.Name
		}
		aliases[name] = importPath
	}
	return aliases
}

func isDirectWorkCall(call *ast.CallExpr, imports map[string]string) bool {
	selector, ok := call.Fun.(*ast.SelectorExpr)
	if !ok {
		return false
	}
	ident, ok := selector.X.(*ast.Ident)
	if !ok || ident.Obj != nil {
		return false
	}
	importPath, ok := imports[ident.Name]
	if !ok {
		return false
	}
	forbidden := map[string]bool{
		"os": true,
		"io/ioutil": true,
		"encoding/json": true,
		"database/sql": true,
		"net/http": true,
	}
	return forbidden[importPath]
}
