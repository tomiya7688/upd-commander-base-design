package main

import (
	"os"
	"path/filepath"
	"strings"
	"testing"

	"upd_commander_checker/internal/checker"
)

func TestBaselineCLIWriteCompareAndResolve(t *testing.T) {
	root := t.TempDir()
	source := filepath.Join(root, "commander.go")
	report := filepath.Join(root, "report.txt")
	baseline := filepath.Join(root, ".upd-baseline.json")
	writeSource := func(contents string) {
		t.Helper()
		if err := os.WriteFile(source, []byte(contents), 0o644); err != nil {
			t.Fatal(err)
		}
	}
	enabledRules := []string{"UPD203"}
	config := checker.Config{Input: root, Output: report, EnabledRules: &enabledRules}
	writeSource("package commander\nimport \"os\"\nfunc Commander() {\n os.Exit(0)\n}\n")
	if code := runCLI([]string{"--write-baseline"}, config); code != 1 {
		t.Fatalf("expected finding exit code 1, got %d", code)
	}
	if _, err := os.Stat(baseline); err != nil {
		t.Fatalf("baseline was not written: %v", err)
	}
	writeSource("\npackage commander\nimport \"os\"\nfunc Commander() {\n os.Exit(0)\n}\n")
	if code := runCLI([]string{"--baseline", baseline}, config); code != 1 {
		t.Fatalf("expected existing finding exit code 1, got %d", code)
	}
	data, err := os.ReadFile(report)
	if err != nil {
		t.Fatal(err)
	}
	if !strings.Contains(string(data), "EXISTING E UPD203") {
		t.Fatalf("expected EXISTING finding in report, got %s", data)
	}
	writeSource("package commander\nimport \"os\"\nfunc Commander() {\n os.Exit(0)\n os.Remove(\"x\")\n}\n")
	if code := runCLI([]string{"--baseline", baseline}, config); code != 1 {
		t.Fatalf("expected new finding exit code 1, got %d", code)
	}
	data, err = os.ReadFile(report)
	if err != nil {
		t.Fatal(err)
	}
	if !strings.Contains(string(data), "EXISTING E UPD203") || !strings.Contains(string(data), "NEW E UPD203") {
		t.Fatalf("expected EXISTING and NEW findings in report, got %s", data)
	}
	writeSource("package commander\nfunc Commander() {}\n")
	if code := runCLI([]string{"--baseline", baseline}, config); code != 0 {
		t.Fatalf("expected resolved-only success exit code 0, got %d", code)
	}
	data, err = os.ReadFile(report)
	if err != nil {
		t.Fatal(err)
	}
	if strings.Count(string(data), "RESOLVED E UPD203") != 1 {
		t.Fatalf("expected RESOLVED finding in report, got %s", data)
	}
}

func TestBaselineCLIRejectsCorruptAndConflictingFlags(t *testing.T) {
	root := t.TempDir()
	input := filepath.Join(root, "source.go")
	if err := os.WriteFile(input, []byte("package sample\n"), 0o644); err != nil {
		t.Fatal(err)
	}
	config := checker.Config{Input: input}
	if code := runCLI([]string{"--write-baseline", "--baseline", "broken.json"}, config); code != 2 {
		t.Fatalf("expected conflicting option exit code 2, got %d", code)
	}
	if code := runCLI([]string{"--baseline", filepath.Join(root, "missing.json")}, config); code != 2 {
		t.Fatalf("expected malformed/missing baseline exit code 2, got %d", code)
	}
}

func TestBaselineCLINewScopeAndRequiresBaseline(t *testing.T) {
	root := t.TempDir()
	source := filepath.Join(root, "commander.go")
	if err := os.WriteFile(source, []byte("package commander\nimport \"os\"\nfunc Commander() { os.Exit(0) }\n"), 0o644); err != nil {
		t.Fatal(err)
	}
	rules := []string{"UPD203"}
	reportPath := filepath.Join(root, "report.txt")
	config := checker.Config{Input: root, Output: reportPath, EnabledRules: &rules}
	baseline := filepath.Join(root, ".upd-baseline.json")
	if code := runCLI([]string{"--write-baseline"}, config); code != 1 {
		t.Fatalf("expected initial finding failure, got %d", code)
	}
	if code := runCLI([]string{"--baseline", baseline, "--fail-on-scope", "new"}, config); code != 0 {
		t.Fatalf("expected existing finding to pass new-only gate, got %d", code)
	}
	report, err := os.ReadFile(reportPath)
	if err != nil || !strings.Contains(string(report), "OK e=1 w=0 a=0") {
		t.Fatalf("expected all existing findings in success summary, got %s (err=%v)", report, err)
	}
	if err := os.WriteFile(source, []byte("package commander\nimport \"os\"\nfunc Commander() { os.Exit(0) }\nfunc Another() { os.Remove(\"x\") }\n"), 0o644); err != nil {
		t.Fatal(err)
	}
	if code := runCLI([]string{"--baseline", baseline, "--fail-on-scope", "new"}, config); code != 1 {
		t.Fatalf("expected new finding to fail new-only gate, got %d", code)
	}
	report, err = os.ReadFile(reportPath)
	if err != nil {
		t.Fatalf("read report: %v", err)
	}
	if !strings.Contains(string(report), "EXISTING E UPD203") || !strings.Contains(string(report), "NEW E UPD203") {
		t.Fatalf("expected all findings to remain reported, got %s", report)
	}
	if code := runCLI([]string{"--baseline", baseline, "--fail-on", "warning", "--severity-override", "UPD203=warning", "--fail-on-scope", "new"}, config); code != 1 {
		t.Fatalf("expected NEW warning to fail when warning is selected, got %d", code)
	}
	report, err = os.ReadFile(reportPath)
	if err != nil || !strings.Contains(string(report), "NEW W UPD203") {
		t.Fatalf("expected NEW warning to remain reported, got %s (err=%v)", report, err)
	}
	if code := runCLI([]string{"--fail-on-scope", "new"}, config); code != 2 {
		t.Fatalf("expected missing-baseline configuration error, got %d", code)
	}
}
