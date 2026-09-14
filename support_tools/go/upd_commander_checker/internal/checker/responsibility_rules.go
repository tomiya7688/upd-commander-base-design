package checker

import (
	"go/ast"
	"go/token"
)

const maxResponsibilityLines = 250
const maxResponsibilityMethods = 12

type responsibilityStats struct {
	line    int
	lines   int
	methods int
}

func checkResponsibilities(file *ast.File, fset *token.FileSet, path string, lines []string, rules []IgnoreRule) []Finding {
	var findings []Finding
	stats := collectResponsibilityStats(file, fset)

	if len(stats) == 0 && len(lines) > maxResponsibilityLines {
		addFinding(
			&findings,
			path,
			1,
			"UPD401",
			"file/module approximation is too large for one responsibility",
			"warning",
			lines,
			rules,
		)
	}

	for name, stat := range stats {
		if stat.lines <= maxResponsibilityLines && stat.methods <= maxResponsibilityMethods {
			continue
		}
		addFinding(
			&findings,
			path,
			stat.line,
			"UPD401",
			"type "+name+" is too large for one responsibility",
			"warning",
			lines,
			rules,
		)
	}
	return findings
}

func collectResponsibilityStats(file *ast.File, fset *token.FileSet) map[string]responsibilityStats {
	stats := map[string]responsibilityStats{}
	for _, declaration := range file.Decls {
		switch value := declaration.(type) {
		case *ast.GenDecl:
			if value.Tok != token.TYPE {
				continue
			}
			for _, spec := range value.Specs {
				typeSpec, ok := spec.(*ast.TypeSpec)
				if !ok {
					continue
				}
				start := fset.Position(typeSpec.Pos()).Line
				end := fset.Position(typeSpec.End()).Line
				stat := stats[typeSpec.Name.Name]
				stat.line = firstLine(stat.line, start)
				stat.lines += spanLines(start, end)
				stats[typeSpec.Name.Name] = stat
			}
		case *ast.FuncDecl:
			if value.Recv == nil || len(value.Recv.List) == 0 {
				continue
			}
			name := receiverName(value.Recv.List[0].Type)
			if name == "" {
				continue
			}
			start := fset.Position(value.Pos()).Line
			end := fset.Position(value.End()).Line
			stat := stats[name]
			stat.line = firstLine(stat.line, start)
			stat.lines += spanLines(start, end)
			stat.methods++
			stats[name] = stat
		}
	}
	return stats
}

func receiverName(expression ast.Expr) string {
	switch value := expression.(type) {
	case *ast.Ident:
		return value.Name
	case *ast.StarExpr:
		return receiverName(value.X)
	case *ast.IndexExpr:
		return receiverName(value.X)
	case *ast.IndexListExpr:
		return receiverName(value.X)
	default:
		return ""
	}
}

func firstLine(current int, candidate int) int {
	if current == 0 || candidate < current {
		return candidate
	}
	return current
}

func spanLines(start int, end int) int {
	if end < start {
		return 1
	}
	return end - start + 1
}
