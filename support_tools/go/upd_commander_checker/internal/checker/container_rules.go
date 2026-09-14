package checker

import (
	"go/ast"
	"go/token"
)

const containerBloatOperationThreshold = 3
const containerBloatExcessSlotThreshold = 6

func checkContainerBoundaries(file *ast.File, fset *token.FileSet, source ModuleInfo, rel string, lines []string, rules []IgnoreRule) []Finding {
	if source.Role != "commander" && source.Role != "messenger" && source.Role != "processing" {
		return nil
	}

	var findings []Finding
	offendingOperations := 0
	excessSlots := 0
	firstLine := 1

	for _, decl := range file.Decls {
		fn, ok := decl.(*ast.FuncDecl)
		if !ok || fn.Recv == nil || !ast.IsExported(fn.Name.Name) {
			continue
		}
		line := fset.Position(fn.Pos()).Line
		if firstLine == 1 {
			firstLine = line
		}

		offends := false
		inputCount := fieldCount(fn.Type.Params)
		if inputCount > 1 {
			offends = true
			excessSlots += inputCount - 1
			addFinding(&findings, rel, line, "UPD301", "multiple inputs reduce readability; consider one Input Container", "attention", lines, rules)
		}

		outputCount := fieldCount(fn.Type.Results)
		if outputCount > 1 {
			offends = true
			excessSlots += outputCount - 1
			addFinding(&findings, rel, line, "UPD302", "multiple return values reduce readability; consider one Output Container", "attention", lines, rules)
		}

		if offends {
			offendingOperations++
		}
	}

	if (source.Role == "commander" || source.Role == "messenger") &&
		(offendingOperations >= containerBloatOperationThreshold || excessSlots >= containerBloatExcessSlotThreshold) {
		addFinding(&findings, rel, firstLine, "UPD303", "uncontainerized signatures contribute to Commander/Messenger bloat", "warning", lines, rules)
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
