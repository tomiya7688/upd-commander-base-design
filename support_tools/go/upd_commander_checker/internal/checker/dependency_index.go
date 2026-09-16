package checker

import (
	"path/filepath"
	"sort"
	"strings"
)

func internalPackagePaths(paths []string, root string) []string {
	packages := map[string]bool{}
	for _, path := range paths {
		relative, err := filepath.Rel(root, filepath.Dir(path))
		if err != nil {
			continue
		}
		relative = filepath.ToSlash(relative)
		if relative == "." || relative == "" || relative == ".." || strings.HasPrefix(relative, "../") {
			continue
		}
		packages[relative] = true
	}

	result := make([]string, 0, len(packages))
	for packagePath := range packages {
		result = append(result, packagePath)
	}
	sort.Strings(result)
	return result
}

func isInternalImport(name string, packages []string) bool {
	normalized := strings.Trim(filepath.ToSlash(name), "/")
	for _, packagePath := range packages {
		if normalized == packagePath || strings.HasSuffix(normalized, "/"+packagePath) {
			return true
		}
	}
	return false
}
