package checker

import (
	"bufio"
	"go/ast"
	"go/parser"
	"go/token"
	"os"
	"path/filepath"
	"sort"
	"strings"
)

func ScanPath(target string, cliIgnore []string) []Finding {
	root := target
	info, err := os.Stat(target)
	if err == nil && !info.IsDir() {
		root = filepath.Dir(target)
	}
	rules := LoadIgnoreRules(root)
	var findings []Finding
	_ = filepath.WalkDir(target, func(path string, entry os.DirEntry, walkErr error) error {
		if walkErr != nil {
			rel, relErr := filepath.Rel(root, path)
			if relErr != nil { rel = path }
			findings = append(findings, Finding{Path: filepath.ToSlash(rel), Line: 1, Code: "UPD001", Message: "read failed", Severity: "error"})
			return nil
		}
		if entry == nil || entry.IsDir() || filepath.Ext(path) != ".go" { return nil }
		rel, relErr := filepath.Rel(root, path)
		if relErr != nil { rel = path }
		relText := filepath.ToSlash(rel)
		if pathIgnored(relText, cliIgnore) { return nil }
		findings = append(findings, scanFile(path, relText, rules)...)
		return nil
	})
	sort.Slice(findings, func(i, j int) bool {
		if findings[i].Path != findings[j].Path { return findings[i].Path < findings[j].Path }
		if findings[i].Line != findings[j].Line { return findings[i].Line < findings[j].Line }
		return findings[i].Code < findings[j].Code
	})
	return findings
}

func scanFile(path string, rel string, rules []IgnoreRule) []Finding {
	fset := token.NewFileSet()
	file, err := parser.ParseFile(fset, path, nil, parser.ParseComments)
	if err != nil {
		return []Finding{{Path: rel, Line: 1, Code: "UPD002", Message: "syntax error", Severity: "error"}}
	}
	lines := readLines(path)
	source := ClassifyPath(rel)
	var findings []Finding
	for _, spec := range file.Imports {
		name := strings.Trim(spec.Path.Value, "\"")
		target := ClassifyImport(name)
		line := fset.Position(spec.Pos()).Line
		if message := DependencyError(source, target); message != "" {
			code := "UPD101"
			if message == "cross-application internal dependency" { code = "UPD102" }
			addFinding(&findings, rel, line, code, message, "error", lines, rules)
		} else if warning := DataCommanderWarning(source, target); warning != "" {
			addFinding(&findings, rel, line, "UPD103", warning, "warning", lines, rules)
		}
	}
	if source.Role == "commander" {
		ast.Inspect(file, func(node ast.Node) bool {
			switch value := node.(type) {
			case *ast.ForStmt, *ast.RangeStmt:
				addFinding(&findings, rel, fset.Position(node.Pos()).Line, "UPD201", "Commander loop", "warning", lines, rules)
			case *ast.BinaryExpr:
				addFinding(&findings, rel, fset.Position(node.Pos()).Line, "UPD202", "Commander calculation", "warning", lines, rules)
			case *ast.CallExpr:
				if isDirectWorkCall(value) { addFinding(&findings, rel, fset.Position(node.Pos()).Line, "UPD203", "Commander direct I/O/API call", "error", lines, rules) }
			}
			return true
		})
	}
	findings = append(findings, checkContainerBoundaries(file, fset, source, rel, lines, rules)...)
	return findings
}

func isDirectWorkCall(call *ast.CallExpr) bool {
	selector, ok := call.Fun.(*ast.SelectorExpr)
	if !ok { return false }
	ident, ok := selector.X.(*ast.Ident)
	if !ok { return false }
	forbidden := map[string]bool{"os": true, "ioutil": true, "json": true, "sql": true, "http": true}
	return forbidden[ident.Name]
}

func addFinding(findings *[]Finding, path string, line int, code string, message string, severity string, lines []string, rules []IgnoreRule) {
	if IsIgnored(path, code, lineAt(lines, line), rules) { return }
	*findings = append(*findings, Finding{Path: path, Line: line, Code: code, Message: message, Severity: severity})
}

func readLines(path string) []string {
	file, err := os.Open(path)
	if err != nil { return nil }
	defer file.Close()
	var lines []string
	scanner := bufio.NewScanner(file)
	for scanner.Scan() { lines = append(lines, scanner.Text()) }
	return lines
}

func lineAt(lines []string, line int) string {
	if line <= 0 || line > len(lines) { return "" }
	return lines[line-1]
}

func pathIgnored(path string, patterns []string) bool {
	for _, pattern := range patterns { if globMatch(path, pattern) { return true } }
	return false
}

func globMatch(path string, pattern string) bool {
	pattern = filepath.ToSlash(pattern)
	if strings.HasSuffix(pattern, "/**") { return strings.HasPrefix(path, strings.TrimSuffix(pattern, "**")) }
	matched, _ := filepath.Match(pattern, path)
	return matched
}
