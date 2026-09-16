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
	targetIsFile := err == nil && !info.IsDir()
	if targetIsFile {
		root = filepath.Dir(target)
	}
	contextRoot := root
	if targetIsFile {
		contextRoot = singleFileContextRoot(target)
	}

	rules, ignoreErr := loadIgnoreRules(root)
	if ignoreErr != nil {
		return []Finding{{Path: ".updcommanderignore", Line: 1, Code: "UPD001", Message: "read failed: " + ignoreErr.Error(), Severity: "error"}}
	}
	var findings []Finding
	var paths []string
	walkErr := filepath.WalkDir(target, func(path string, entry os.DirEntry, walkErr error) error {
		if walkErr != nil {
			rel, relErr := filepath.Rel(root, path)
			if relErr != nil {
				rel = path
			}
			findings = append(findings, Finding{Path: filepath.ToSlash(rel), Line: 1, Code: "UPD001", Message: "read failed: " + walkErr.Error(), Severity: "error"})
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
		if pathIgnored(relText, cliIgnore) || IsPathIgnored(relText, rules) {
			return nil
		}
		paths = append(paths, path)
		return nil
	})
	if walkErr != nil {
		findings = append(findings, Finding{Path: filepath.ToSlash(target), Line: 1, Code: "UPD001", Message: "read failed: " + walkErr.Error(), Severity: "error"})
	}

	dependencyPaths := paths
	if targetIsFile {
		dependencyPaths = dependencyContextPaths(contextRoot)
	}
	internalPackages := internalPackagePaths(dependencyPaths, contextRoot)
	for _, path := range paths {
		rel, relErr := filepath.Rel(root, path)
		if relErr != nil {
			rel = path
		}
		classificationRel, classificationErr := filepath.Rel(contextRoot, path)
		if classificationErr != nil {
			classificationRel = path
		}
		findings = append(
			findings,
			scanFile(path, filepath.ToSlash(rel), filepath.ToSlash(classificationRel), rules, internalPackages)...,
		)
	}
	findings = append(findings, checkDataTypeLocations(paths, root, rules)...)
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

func scanFile(path string, rel string, classificationPath string, rules []IgnoreRule, internalPackages []string) []Finding {
	fset := token.NewFileSet()
	file, err := parser.ParseFile(fset, path, nil, parser.ParseComments)
	if err != nil {
		return []Finding{{Path: rel, Line: 1, Code: "UPD002", Message: "syntax error", Severity: "error"}}
	}
	lines := readLines(path)
	source := ClassifyPath(classificationPath)
	var findings []Finding
	findings = append(findings, checkDependencies(file, fset, source, rel, lines, rules, internalPackages)...)
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
