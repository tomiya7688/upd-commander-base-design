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
	return ScanPathWithAllThresholds(target, cliIgnore, 2, 12, 80, 3, 2)
}

func ScanPathWithUpd301MaxInputs(target string, cliIgnore []string, upd301MaxInputs int) []Finding {
	return ScanPathWithAllThresholds(target, cliIgnore, upd301MaxInputs, 12, 80, 3, 2)
}

func ScanPathWithThresholds(target string, cliIgnore []string, upd301MaxInputs int, flatLayerMinFiles int, flatLayerMinDirectPercent int) []Finding {
	return ScanPathWithAllThresholds(target, cliIgnore, upd301MaxInputs, flatLayerMinFiles, flatLayerMinDirectPercent, 3, 2)
}

func ScanPathWithAllThresholds(target string, cliIgnore []string, upd301MaxInputs int, flatLayerMinFiles int, flatLayerMinDirectPercent int, modelGroupMinItems int, modelGroupMinOccurrences int) []Finding {
	return ScanPathWithCommonRoots(target, cliIgnore, upd301MaxInputs, flatLayerMinFiles, flatLayerMinDirectPercent, modelGroupMinItems, modelGroupMinOccurrences, []string{"common", "shared"})
}

func ScanPathWithCommonRoots(target string, cliIgnore []string, upd301MaxInputs int, flatLayerMinFiles int, flatLayerMinDirectPercent int, modelGroupMinItems int, modelGroupMinOccurrences int, commonRoots []string) []Finding {
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
	modelOccurrences := []ModelGroupOccurrence{}
	for _, path := range paths {
		rel, relErr := filepath.Rel(root, path)
		if relErr != nil {
			rel = path
		}
		classificationRel, classificationErr := filepath.Rel(contextRoot, path)
		if classificationErr != nil {
			classificationRel = path
		}
		relativeText := filepath.ToSlash(rel)
		classificationText := filepath.ToSlash(classificationRel)
		findings = append(
			findings,
			scanFile(path, relativeText, classificationText, rules, internalPackages, upd301MaxInputs)...,
		)
		modelOccurrences = append(
			modelOccurrences,
			collectModelGroupOccurrences(path, relativeText, classificationText, rules, modelGroupMinItems)...,
		)
	}
	findings = append(findings, checkDataTypeLocations(paths, root, rules)...)
	findings = append(findings, checkCommonUsage(paths, root, commonRoots, rules)...)
	findings = append(findings, modelAttentionFindings(modelOccurrences, modelGroupMinOccurrences)...)
	if !targetIsFile {
		findings = append(findings, checkFlatLayers(paths, root, rules, flatLayerMinFiles, flatLayerMinDirectPercent)...)
	}
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

func scanFile(path string, rel string, classificationPath string, rules []IgnoreRule, internalPackages []string, upd301MaxInputs int) []Finding {
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
	findings = append(findings, checkContainerBoundaries(file, fset, source, rel, lines, rules, upd301MaxInputs)...)
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

var flatLayerExcludedDirs = map[string]bool{
	"generated":   true,
	"third_party": true,
	"vendor":      true,
	"external":    true,
	"build":       true,
}

type flatLayerCount struct {
	total  int
	direct int
}

func checkFlatLayers(paths []string, root string, rules []IgnoreRule, minFiles int, minDirectPercent int) []Finding {
	counts := map[string]flatLayerCount{}
	for _, path := range paths {
		relative, err := filepath.Rel(root, path)
		if err != nil {
			continue
		}
		relative = filepath.ToSlash(relative)
		parts := strings.Split(relative, "/")
		if hasExcludedFlatLayerDirectory(parts) {
			continue
		}
		layerIndex := flatLayerRootIndex(parts)
		if layerIndex < 0 {
			continue
		}
		layerRoot := strings.Join(parts[:layerIndex+1], "/")
		count := counts[layerRoot]
		count.total++
		if len(parts) == layerIndex+2 {
			count.direct++
		}
		counts[layerRoot] = count
	}

	findings := []Finding{}
	for layerRoot, count := range counts {
		if count.total < minFiles || count.direct*100 < count.total*minDirectPercent {
			continue
		}
		if IsIgnored(layerRoot, "UPD405", "", rules) {
			continue
		}
		findings = append(findings, Finding{
			Path:     layerRoot,
			Line:     1,
			Code:     "UPD405",
			Message:  "large flat layer reduces navigability; consider grouping related responsibilities",
			Severity: "attention",
		})
	}
	return findings
}

func hasExcludedFlatLayerDirectory(parts []string) bool {
	for _, part := range parts[:max(0, len(parts)-1)] {
		if flatLayerExcludedDirs[strings.ToLower(part)] {
			return true
		}
	}
	return false
}

func flatLayerRootIndex(parts []string) int {
	scopeStart := 0
	for index := 0; index+2 < len(parts); index++ {
		if appRoots[strings.ToLower(parts[index])] {
			scopeStart = index + 2
		}
	}
	layerIndex := -1
	for index := scopeStart; index+1 < len(parts); index++ {
		if layerNames[strings.ToLower(parts[index])] {
			layerIndex = index
		}
	}
	return layerIndex
}
