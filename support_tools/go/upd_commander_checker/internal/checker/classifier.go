package checker

import (
	"path/filepath"
	"strings"
)

var layerNames = map[string]bool{"ui": true, "process": true, "data": true}
var roleNames = map[string]bool{"commander": true, "messenger": true, "processing": true, "compresser": true}
var appRoots = map[string]bool{"app": true, "apps": true, "application": true, "applications": true, "feature": true, "features": true}

func ClassifyPath(path string) ModuleInfo {
	directories := pathDirectories(path)
	stem := fileStem(path)
	return ModuleInfo{Path: path, Layer: findName(directories, layerNames), Role: findPathRole(directories, stem), ApplicationID: findApplication(directories)}
}

func ClassifyImport(name string) ModuleInfo {
	parts := splitParts(name)
	return ModuleInfo{Path: name, Layer: findName(parts, layerNames), Role: findRole(parts), ApplicationID: findApplication(parts)}
}

func pathDirectories(value string) []string {
	normalized := strings.ToLower(strings.ReplaceAll(value, "\\", "/"))
	directory := filepath.ToSlash(filepath.Dir(normalized))
	if directory == "." {
		return nil
	}
	return strings.FieldsFunc(directory, func(r rune) bool { return r == '/' })
}

func fileStem(value string) string {
	name := filepath.Base(strings.ReplaceAll(value, "\\", "/"))
	extension := filepath.Ext(name)
	return strings.ToLower(strings.TrimSuffix(name, extension))
}

func splitParts(value string) []string {
	replacer := strings.NewReplacer("\\", "/", ".", "/", "-", "_", "_", "/")
	normalized := strings.ToLower(replacer.Replace(value))
	return strings.FieldsFunc(normalized, func(r rune) bool { return r == '/' })
}

func findName(parts []string, candidates map[string]bool) string {
	for _, part := range parts {
		if candidates[part] {
			return part
		}
	}
	return ""
}

func findPathRole(directories []string, stem string) string {
	if role := findName(directories, roleNames); role != "" {
		return role
	}
	for role := range roleNames {
		if stem == role || strings.HasSuffix(stem, "_"+role) {
			return role
		}
	}
	return ""
}

func findRole(parts []string) string {
	for _, part := range parts {
		if roleNames[part] {
			return part
		}
		if strings.HasSuffix(part, "commander") {
			return "commander"
		}
		if strings.HasSuffix(part, "messenger") {
			return "messenger"
		}
		if strings.HasSuffix(part, "processing") {
			return "processing"
		}
		if strings.HasSuffix(part, "compresser") {
			return "compresser"
		}
	}
	return ""
}

func findApplication(parts []string) string {
	for index, part := range parts {
		if appRoots[part] && index+1 < len(parts) {
			return parts[index+1]
		}
	}
	return ""
}
