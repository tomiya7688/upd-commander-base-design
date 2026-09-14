package checker

import (
	"go/ast"
	"go/token"
)

func checkContainerBoundaries(file *ast.File, fset *token.FileSet, source ModuleInfo, rel string, lines []string, rules []IgnoreRule) []Finding {
	if source.Role != "commander" && source.Role != "messenger" && source.Role != "processing" {
		return nil
	}
	var findings []Finding
	for _, decl := range file.Decls {
		fn, ok := decl.(*ast.FuncDecl)
		if !ok || fn.Recv == nil || !ast.IsExported(fn.Name.Name) {
			continue
		}
		line := fset.Position(fn.Pos()).Line
		if fieldCount(fn.Type.Params) > 1 {
			addFinding(&findings, rel, line, "UPD301", "class operation has multiple inputs; use one Input Container", "warning", lines, rules)
		}
		if fieldCount(fn.Type.Results) > 1 {
			addFinding(&findings, rel, line, "UPD302", "class operation returns multiple values; use one Output Container", "warning", lines, rules)
		}
	}
	return findings
}

func fieldCount(list *ast.FieldList) int {
	if list == nil {
		return 0
	}
	count := 0
	for _, field := range list.List {
		if len(field.Names) == 0 {
			count++
		} else {
			count += len(field.Names)
		}
	}
	return count
}
