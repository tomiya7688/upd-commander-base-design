package checker

import (
	"os"
	"path/filepath"
	"strings"
)

func singleFileContextRoot(path string) string {
	directory := filepath.Dir(path)
	var applicationRoot string
	for current := directory; ; current = filepath.Dir(current) {
		name := strings.ToLower(filepath.Base(current))
		if appRoots[name] {
			applicationRoot = filepath.Dir(current)
		}
		parent := filepath.Dir(current)
		if parent == current {
			break
		}
	}
	if applicationRoot != "" {
		return applicationRoot
	}

	for current := directory; ; current = filepath.Dir(current) {
		if layerNames[strings.ToLower(filepath.Base(current))] {
			return filepath.Dir(current)
		}
		parent := filepath.Dir(current)
		if parent == current {
			break
		}
	}
	return directory
}

func dependencyContextPaths(root string) []string {
	var paths []string
	_ = filepath.WalkDir(root, func(path string, entry os.DirEntry, err error) error {
		if err != nil || entry == nil || entry.IsDir() || filepath.Ext(path) != ".go" {
			return nil
		}
		paths = append(paths, path)
		return nil
	})
	return paths
}
