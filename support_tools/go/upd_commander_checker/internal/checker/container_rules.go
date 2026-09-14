package checker

import (
	"go/ast"
	"go/token"
)

const containerMinReducibleLines = 10
const containerMinReductionRatio = 0.20

func checkContainerBoundaries(file *ast.File, fset *token.FileSet, source ModuleInfo, rel string, lines []string, rules []IgnoreRule) []Finding {
	if source.Role != "commander" && source.Role != "messenger" && source.Role != "processing" {
		return nil
	}

	var findings []Finding
	reducibleLines := 0
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

		inputCount := fieldCount(fn.Type.Params)
		outputCount := fieldCount(fn.Type.Results)
		inputViolation := inputCount > 1
		outputViolation := outputCount > 1

		if inputViolation {
			addFinding(&findings, rel, line, "UPD301", "multiple inputs reduce readability; consider one Input Container", "attention", lines, rules)
		}
		if outputViolation {
			addFinding(&findings, rel, line, "UPD302", "multiple return values reduce readability; consider one Output Container", "attention", lines, rules)
		}

		if inputViolation || outputViolation {
			signatureLines := signatureReducibleLines(fn, fset)
			excessValues := maxInt(0, inputCount-1) + maxInt(0, outputCount-1)
			reducibleLines += maxInt(signatureLines, excessValues)
		}
	}

	if (source.Role == "commander" || source.Role == "messenger") && largeCompressionExpected(reducibleLines, len(lines)) {
		addFinding(&findings, rel, firstLine, "UPD303", "Compresser/Container introduction is expected to substantially reduce this Commander/Messenger", "warning", lines, rules)
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

func signatureReducibleLines(fn *ast.FuncDecl, fset *token.FileSet) int {
	start := fset.Position(fn.Pos()).Line
	end := start
	if fn.Type.Params != nil {
		end = maxInt(end, fset.Position(fn.Type.Params.Closing).Line)
	}
	if fn.Type.Results != nil {
		end = maxInt(end, fset.Position(fn.Type.Results.Closing).Line)
	}
	return maxInt(0, end-start)
}

func largeCompressionExpected(reducibleLines int, totalLines int) bool {
	if reducibleLines < containerMinReducibleLines {
		return false
	}
	return float64(reducibleLines)/float64(maxInt(1, totalLines)) >= containerMinReductionRatio
}

func maxInt(left int, right int) int {
	if left > right {
		return left
	}
	return right
}
