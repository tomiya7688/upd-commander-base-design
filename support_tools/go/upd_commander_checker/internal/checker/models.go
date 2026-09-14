package checker

type Finding struct {
	Path     string
	Line     int
	Code     string
	Message  string
	Severity string
}

type ModuleInfo struct {
	Path          string
	Layer         string
	Role          string
	ApplicationID string
}

type DependencyRuleResult struct {
	Code     string
	Message  string
	Severity string
}
