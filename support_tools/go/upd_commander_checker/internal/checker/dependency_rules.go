package checker

var boundaryAPINames = map[string]bool{
	"contract":  true,
	"contracts": true,
	"dto":       true,
	"dtos":      true,
	"shared":    true,
}

func DependencyError(source ModuleInfo, target ModuleInfo) string {
	if source.ApplicationID != "" && target.ApplicationID != "" && source.ApplicationID != target.ApplicationID {
		if !isApplicationBoundaryAPI(target) {
			return "cross-application internal dependency"
		}
	}
	if source.Layer == "ui" && target.Layer == "data" {
		return "UI must not depend on Data"
	}
	if source.Layer == "data" && target.Layer == "ui" {
		return "Data must not depend on UI"
	}
	if source.Role == "messenger" && target.Role == "processing" {
		return "Messenger must not depend on Processing"
	}
	if source.Role == "processing" && target.Role == "processing" {
		return "Processing must not depend on Processing"
	}
	if source.Role == "commander" && target.Role == "processing" && source.Layer != "" && target.Layer != "" && source.Layer != target.Layer {
		return "Commander must not depend on Processing in another layer"
	}
	return ""
}

func isApplicationBoundaryAPI(target ModuleInfo) bool {
	if target.Role == "messenger" {
		return true
	}
	for _, part := range splitParts(target.Path) {
		if boundaryAPINames[part] {
			return true
		}
	}
	return false
}
