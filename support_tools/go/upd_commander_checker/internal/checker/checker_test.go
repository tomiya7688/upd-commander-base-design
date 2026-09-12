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
	assertNoCode(t, findings, "UPD102")
}

func TestBoundaryLikeDirectoryIsNotBoundaryAPI(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "applications", "main", "process", "main_commander.go")
	writeTestFile(t, path, "package process\nimport _ \"example/applications/settings/contractor/process/settings_processing\"\n")
	findings := ScanPath(root, nil)
	assertHasCode(t, findings, "UPD102")
}

func TestBoundaryAPILayerViolationKeepsUPD101(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "applications", "main", "ui", "screen_processing.go")
	writeTestFile(t, path, "package ui\nimport _ \"example/applications/settings/shared/data/storage\"\n")
	findings := ScanPath(root, nil)
	assertHasCode(t, findings, "UPD101")
	assertNoCode(t, findings, "UPD102")
}

func TestCheckerPackageNameDoesNotMakeHelpersCommander(t *testing.T) {
	module := ClassifyPath("support_tools/go/upd_commander_checker/internal/checker/scanner.go")
	if module.Role != "" {
		t.Fatalf("unexpected role: %s", module.Role)
	}
}

func TestInlineIgnoreSuppressesCommanderCalculation(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "process", "fast_commander.go")
	writeTestFile(t, path, "package process\nfunc run(a, b int) int { return a + b // upd: ignore UPD202 - performance\n}\n")
	findings := ScanPath(root, nil)
	assertNoCode(t, findings, "UPD202")
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

func assertNoCode(t *testing.T, findings []Finding, code string) {
	t.Helper()
	for _, finding := range findings {
		if finding.Code == code {
			t.Fatalf("unexpected %s: %+v", code, finding)
		}
	}
}
