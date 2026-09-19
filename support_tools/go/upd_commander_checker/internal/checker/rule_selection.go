package checker

import "fmt"

var SupportedRules = []string{
	"UPD001", "UPD002",
	"UPD101", "UPD102", "UPD103",
	"UPD201", "UPD202", "UPD203",
	"UPD301", "UPD302", "UPD303",
	"UPD401", "UPD402", "UPD403", "UPD404", "UPD405", "UPD406",
}

var supportedRuleSet = func() map[string]struct{} {
	values := make(map[string]struct{}, len(SupportedRules))
	for _, code := range SupportedRules {
		values[code] = struct{}{}
	}
	return values
}()

func ValidateEnabledRules(values []string) error {
	for _, code := range values {
		if _, ok := supportedRuleSet[code]; !ok {
			return fmt.Errorf("unsupported UPD code: %s", code)
		}
	}
	return nil
}

func FilterEnabledFindings(findings []Finding, enabledRules *[]string) []Finding {
	if enabledRules == nil {
		return findings
	}
	enabled := make(map[string]struct{}, len(*enabledRules))
	for _, code := range *enabledRules {
		enabled[code] = struct{}{}
	}
	filtered := make([]Finding, 0, len(findings))
	for _, finding := range findings {
		if _, ok := enabled[finding.Code]; ok {
			filtered = append(filtered, finding)
		}
	}
	return filtered
}
