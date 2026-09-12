package checker

func DependencyError(source ModuleInfo, target ModuleInfo) string {
	if source.ApplicationID != "" && target.ApplicationID != "" && source.ApplicationID != target.ApplicationID {
		if target.Role != "messenger" && target.Role != "" && target.Role != "contract" {
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
