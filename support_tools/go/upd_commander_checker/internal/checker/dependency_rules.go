package checker

var boundaryAPINames = map[string]bool{
	"contract":  true,
	"contracts": true,
	"dto":       true,
	"dtos":      true,
	"message":   true,
	"messages":  true,
}

func DependencyResult(source ModuleInfo, target ModuleInfo) *DependencyRuleResult {
	if source.ApplicationID != "" && target.ApplicationID != "" && source.ApplicationID != target.ApplicationID {
		if !isApplicationBoundaryAPI(target) {
			return &DependencyRuleResult{Code: "UPD102", Message: "cross-application internal dependency", Severity: "error"}
		}
	}
	if source.Layer == "common" && (target.Layer == "ui" || target.Layer == "process" || target.Layer == "data") {
		return &DependencyRuleResult{Code: "UPD101", Message: "Common/Shared must not depend on layer-specific implementation", Severity: "error"}
	}
	if source.Layer == "ui" && target.Layer == "data" {
		return &DependencyRuleResult{Code: "UPD101", Message: "UI must not depend on Data", Severity: "error"}
	}
	if source.Layer == "data" && target.Layer == "ui" {
		return &DependencyRuleResult{Code: "UPD101", Message: "Data must not depend on UI", Severity: "error"}
	}
	if source.Layer != "common" && target.Layer != "common" && source.Role == "messenger" && target.Role == "processing" {
		return &DependencyRuleResult{Code: "UPD101", Message: "Messenger must not depend on Processing", Severity: "error"}
	}
	if source.Layer != "common" && target.Layer != "common" && source.Role == "processing" && target.Role == "processing" {
		return &DependencyRuleResult{Code: "UPD101", Message: "Processing must not depend on Processing", Severity: "error"}
	}
	if source.Layer != "common" && target.Layer != "common" && source.Role == "commander" && target.Role == "processing" && source.Layer != "" && target.Layer != "" && source.Layer != target.Layer {
		return &DependencyRuleResult{Code: "UPD101", Message: "Commander must not depend on Processing in another layer", Severity: "error"}
	}
	if source.Layer == "data" && source.Role == "commander" && target.Layer == "data" && target.Role == "commander" {
		return &DependencyRuleResult{Code: "UPD103", Message: "Data Commander should not communicate directly with another Data Commander", Severity: "warning"}
	}
	return nil
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
