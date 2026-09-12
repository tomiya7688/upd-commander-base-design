package checker

import (
	"os"
	"path/filepath"
	"testing"
)

func TestUIToDataImportIsReported(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "ui", "screen_processing.go")
	writeTestFile(t, path, "package ui\nimport _ \"example/data/storage\"\n")
	findings := ScanPath(root, nil)
	assertHasCode(t, findings, "UPD101")
}

func TestCrossApplicationInternalImportIsReported(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "applications", "main", "process", "main_commander.go")
	writeTestFile(t, path, "package process\nimport _ \"example/applications/settings/process/settings_processing\"\n")
	findings := ScanPath(root, nil)
	assertHasCode(t, findings, "UPD102")
}

func TestCrossApplicationMessengerIsAllowed(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "applications", "main", "process", "main_commander.go")
	writeTestFile(t, path, "package process\nimport _ \"example/applications/settings/process/settings_messenger\"\n")
	findings := ScanPath(root, nil)
	for _, finding := range findings {
		if finding.Code == "UPD102" {
			t.Fatalf("unexpected UPD102: %+v", finding)
		}
	}
}

func TestInlineIgnoreSuppressesCommanderCalculation(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "process", "fast_commander.go")
	writeTestFile(t, path, "package process\nfunc run(a, b int) int { return a + b // upd: ignore UPD202 - performance\n}\n")
	findings := ScanPath(root, nil)
	for _, finding := range findings {
		if finding.Code == "UPD202" {
			t.Fatalf("unexpected UPD202: %+v", finding)
		}
	}
}

func writeTestFile(t *testing.T, path string, content string) {
	t.Helper()
	if err := os.MkdirAll(filepath.Dir(path), 0o755); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(path, []byte(content), 0o644); err != nil {
		t.Fatal(err)
	}
}

func assertHasCode(t *testing.T, findings []Finding, code string) {
	t.Helper()
	for _, finding := range findings {
		if finding.Code == code {
			return
		}
	}
	t.Fatalf("missing %s: %+v", code, findings)
}
