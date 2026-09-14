package checker

import (
	"bufio"
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
			if relErr != nil {
				rel = path
			}
			findings = append(findings, Finding{Path: filepath.ToSlash(rel), Line: 1, Code: "UPD001", Message: "read failed", Severity: "error"})
			return nil
		}
		if entry == nil || entry.IsDir() || filepath.Ext(path) != ".go" {
			return nil
		}
		rel, relErr := filepath.Rel(root, path)
		if relErr != nil {
			rel = path
		}
		relText := filepath.ToSlash(rel)
		if pathIgnored(relText, cliIgnore) {
			return nil
		}
		findings = append(findings, scanFile(path, relText, rules)...)
		return nil
	})
	sort.Slice(findings, func(i, j int) bool {
		if findings[i].Path != findings[j].Path {
			return findings[i].Path < findings[j].Path
		}
		if findings[i].Line != findings[j].Line {
			return findings[i].Line < findings[j].Line
		}
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
	findings = append(findings, checkDependencies(file, fset, source, rel, lines, rules)...)
	findings = append(findings, checkCommander(file, fset, source, rel, lines, rules)...)
	findings = append(findings, checkContainerBoundaries(file, fset, source, rel, lines, rules)...)
	findings = append(findings, checkResponsibilities(file, fset, rel, lines, rules)...)
	return findings
}

func readLines(path string) []string {
	file, err := os.Open(path)
	if err != nil {
		return nil
	}
	defer file.Close()
	var lines []string
	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		lines = append(lines, scanner.Text())
	}
	return lines
}

func pathIgnored(path string, patterns []string) bool {
	for _, pattern := range patterns {
		if globMatch(path, pattern) {
			return true
		}
	}
	return false
}

func globMatch(path string, pattern string) bool {
	pattern = filepath.ToSlash(pattern)
	if strings.HasSuffix(pattern, "/**") {
		return strings.HasPrefix(path, strings.TrimSuffix(pattern, "**"))
	}
	matched, _ := filepath.Match(pattern, path)
	return matched
}
