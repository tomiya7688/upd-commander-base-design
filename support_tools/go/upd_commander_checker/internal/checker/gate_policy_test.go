package checker

import "testing"

func TestNormalizeFailOn(t *testing.T) {
	values, ok := NormalizeFailOn([]string{" ERROR ", "warning", "error"})
	if !ok || len(values) != 2 || values[0] != "error" || values[1] != "warning" {
		t.Fatalf("unexpected normalized gate: %#v, valid=%t", values, ok)
	}
	if empty, ok := ParseFailOnArgument(""); !ok || len(empty) != 0 {
		t.Fatalf("empty explicit gate not accepted: %#v, valid=%t", empty, ok)
	}
	for _, invalid := range [][]string{{"fatal"}, {""}, {"error", "bad"}} {
		if _, ok := NormalizeFailOn(invalid); ok {
			t.Errorf("accepted invalid gate: %#v", invalid)
		}
	}
}

func TestNormalizeSeverityOverrides(t *testing.T) {
	values, ok := NormalizeSeverityOverrides(map[string]string{"upd101": "WARNING", "UPD9999": "attention"})
	if !ok || values["UPD101"] != "warning" || values["UPD9999"] != "attention" {
		t.Fatalf("unexpected overrides: %#v, valid=%t", values, ok)
	}
	for _, invalid := range []map[string]string{{"bad": "error"}, {"UPD101": "fatal"}, {"UPD10": "error"}, {" UPD101": "error"}, {"UPD101": " warning "}} {
		if _, ok := NormalizeSeverityOverrides(invalid); ok {
			t.Errorf("accepted invalid overrides: %#v", invalid)
		}
	}
	if rule, severity, ok := ParseSeverityOverrideArgument("upd203=warning"); !ok || rule != "UPD203" || severity != "warning" {
		t.Fatalf("unexpected parsed CLI override: %q=%q, valid=%t", rule, severity, ok)
	}
	if _, _, ok := ParseSeverityOverrideArgument("UPD203 warning"); ok {
		t.Fatal("accepted malformed CLI override")
	}
}

func TestApplySeverityOverridesAndShouldFail(t *testing.T) {
	findings := []Finding{{Code: "UPD203", Severity: "error"}, {Code: "UPD301", Severity: "attention"}}
	effective := ApplySeverityOverrides(findings, map[string]string{"UPD203": "warning"})
	if len(effective) != 2 || effective[0].Severity != "warning" || findings[0].Severity != "error" {
		t.Fatalf("override should preserve all findings and original input: %#v %#v", findings, effective)
	}
	if ShouldFail(effective, []string{"error"}) || !ShouldFail(effective, []string{"warning"}) {
		t.Fatal("gate did not use effective severity")
	}
	if ShouldFail(effective, []string{}) {
		t.Fatal("empty explicit gate should pass")
	}
}

func TestGateExceptionMatchesRulePathAndOptionalLine(t *testing.T) {
	line := 5
	exceptions, ok := NormalizeGateExceptions([]GateException{
		{Rule: "upd203", Path: "src/a.go", Line: &line, Reason: "approved"},
		{Rule: "UPD203", Path: "src/a.go", Reason: "file allowance"},
		{Rule: "UPD203", Path: "src/b.go", Reason: "file allowance"},
	})
	if !ok || GateExceptionReason(Finding{Code: "UPD203", Path: "src/a.go", Line: 5}, exceptions) != "approved" {
		t.Fatalf("line exception did not match: %#v", exceptions)
	}
	if GateExceptionReason(Finding{Code: "UPD203", Path: "src/a.go", Line: 6}, exceptions) != "file allowance" {
		t.Fatal("file-wide exception did not apply outside the line-specific match")
	}
	if GateExceptionReason(Finding{Code: "UPD203", Path: "src/b.go", Line: 9}, exceptions) != "file allowance" {
		t.Fatal("file exception did not match every line")
	}
	if _, ok := NormalizeGateExceptions([]GateException{{Rule: "UPD203", Path: "../a.go", Reason: "bad"}}); ok {
		t.Fatal("accepted parent traversal")
	}
}
