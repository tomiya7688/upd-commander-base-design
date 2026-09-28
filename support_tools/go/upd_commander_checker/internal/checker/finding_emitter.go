package checker

import "strings"

func addFinding(findings *[]Finding, path string, line int, code string, message string, severity string, lines []string, rules []IgnoreRule) {
	if IsIgnored(path, code, lineAt(lines, line), rules) {
		return
	}
	context := strings.TrimSpace(lineAt(lines, line))
	if context == "" {
		context = "rule:" + code
	}
	*findings = append(*findings, Finding{Path: path, Line: line, Code: code, Message: message, Severity: severity, Context: context})
}

func lineAt(lines []string, line int) string {
	if line <= 0 || line > len(lines) {
		return ""
	}
	return lines[line-1]
}
