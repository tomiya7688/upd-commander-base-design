package checker

import "testing"

func TestFilterEnabledFindings(t *testing.T) {
	findings := []Finding{
		{Code: "UPD101"},
		{Code: "UPD202"},
	}
	enabled := []string{"UPD202"}
	filtered := FilterEnabledFindings(findings, &enabled)
	if len(filtered) != 1 || filtered[0].Code != "UPD202" {
		t.Fatalf("unexpected findings: %#v", filtered)
	}
}

func TestEmptyEnabledRulesDisableAllFindings(t *testing.T) {
	findings := []Finding{{Code: "UPD101"}}
	enabled := []string{}
	if filtered := FilterEnabledFindings(findings, &enabled); len(filtered) != 0 {
		t.Fatalf("unexpected findings: %#v", filtered)
	}
}

func TestMissingEnabledRulesKeepAllFindings(t *testing.T) {
	findings := []Finding{{Code: "UPD101"}}
	if filtered := FilterEnabledFindings(findings, nil); len(filtered) != 1 {
		t.Fatalf("unexpected findings: %#v", filtered)
	}
}

func TestUnknownEnabledRuleIsRejected(t *testing.T) {
	if err := ValidateEnabledRules([]string{"UPD999"}); err == nil {
		t.Fatal("expected validation error")
	}
}
