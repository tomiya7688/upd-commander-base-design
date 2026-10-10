package checker

import (
	"regexp"
	"strings"
)

var supportedSeverities = map[string]struct{}{
	"error": {}, "warning": {}, "attention": {},
}

var updRulePattern = regexp.MustCompile(`(?i)^UPD[0-9]{3,}$`)

func NormalizeFailOn(values []string) ([]string, bool) {
	result := make([]string, 0, len(values))
	seen := make(map[string]struct{}, len(values))
	for _, value := range values {
		severity := strings.ToLower(strings.TrimSpace(value))
		if _, ok := supportedSeverities[severity]; !ok {
			return nil, false
		}
		if _, exists := seen[severity]; !exists {
			seen[severity] = struct{}{}
			result = append(result, severity)
		}
	}
	return result, true
}

func ParseFailOnArgument(value string) ([]string, bool) {
	if value == "" {
		return []string{}, true
	}
	return NormalizeFailOn(strings.Split(value, ","))
}

func NormalizeSeverityOverrides(values map[string]string) (map[string]string, bool) {
	result := make(map[string]string, len(values))
	for rule, rawSeverity := range values {
		code := strings.ToUpper(rule)
		severity := strings.ToLower(rawSeverity)
		if !updRulePattern.MatchString(code) {
			return nil, false
		}
		if _, ok := supportedSeverities[severity]; !ok {
			return nil, false
		}
		result[code] = severity
	}
	return result, true
}

func ParseSeverityOverrideArgument(value string) (string, string, bool) {
	rule, severity, found := strings.Cut(value, "=")
	if !found {
		return "", "", false
	}
	normalized, valid := NormalizeSeverityOverrides(map[string]string{rule: severity})
	if !valid {
		return "", "", false
	}
	for code, level := range normalized {
		return code, level, true
	}
	return "", "", false
}

func ApplySeverityOverrides(findings []Finding, overrides map[string]string) []Finding {
	if len(overrides) == 0 {
		return findings
	}
	result := make([]Finding, len(findings))
	for index, finding := range findings {
		result[index] = finding
		if severity, ok := overrides[strings.ToUpper(finding.Code)]; ok {
			result[index].Severity = severity
		}
	}
	return result
}

func ShouldFail(findings []Finding, failOn []string) bool {
	gate := make(map[string]struct{}, len(failOn))
	for _, severity := range failOn {
		gate[severity] = struct{}{}
	}
	for _, finding := range findings {
		if _, ok := gate[finding.Severity]; ok {
			return true
		}
	}
	return false
}
