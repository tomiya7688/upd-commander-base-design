package checker

import (
	"bufio"
	"os"
	"path/filepath"
	"strings"
)

type IgnoreRule struct {
	Code    string
	Pattern string
}

func LoadIgnoreRules(root string) []IgnoreRule {
	file, err := os.Open(filepath.Join(root, ".updcommanderignore"))
	if err != nil {
		return nil
	}
	defer file.Close()

	var rules []IgnoreRule
	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := strings.TrimSpace(scanner.Text())
		if line == "" || strings.HasPrefix(line, "#") {
			continue
		}
		mainPart := strings.TrimSpace(strings.SplitN(line, "#", 2)[0])
		fields := strings.Fields(mainPart)
		if len(fields) == 1 {
			rules = append(rules, IgnoreRule{Code: "all", Pattern: fields[0]})
		}
		if len(fields) >= 2 {
			rules = append(rules, IgnoreRule{Code: fields[0], Pattern: fields[1]})
		}
	}
	return rules
}

func IsIgnored(path string, code string, lineText string, rules []IgnoreRule) bool {
	for _, rule := range rules {
		matched, _ := filepath.Match(rule.Pattern, filepath.ToSlash(path))
		if matched && (rule.Code == "all" || rule.Code == code) {
			return true
		}
	}
	marker := "upd: ignore " + code
	return strings.Contains(lineText, marker) || strings.Contains(lineText, "upd: ignore all")
}
