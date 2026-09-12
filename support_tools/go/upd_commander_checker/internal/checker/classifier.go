package checker

import "strings"

var layerNames = map[string]bool{"ui": true, "process": true, "data": true}
var roleNames = map[string]bool{"commander": true, "messenger": true, "processing": true}
var appRoots = map[string]bool{"app": true, "apps": true, "application": true, "applications": true, "feature": true, "features": true}

func ClassifyPath(path string) ModuleInfo {
	parts := splitParts(path)
	return ModuleInfo{
		Path:          path,
		Layer:         findName(parts, layerNames),
		Role:          findRole(parts),
		ApplicationID: findApplication(parts),
	}
}

func ClassifyImport(name string) ModuleInfo {
	parts := splitParts(name)
	return ModuleInfo{
		Path:          name,
		Layer:         findName(parts, layerNames),
		Role:          findRole(parts),
		ApplicationID: findApplication(parts),
	}
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
