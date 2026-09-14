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

func TestNestedApplicationInternalImportIsReported(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "apps", "product", "applications", "settings", "ui", "screen_processing.go")
	writeTestFile(t, path, "package ui\nimport _ \"example/apps/product/applications/profile/process/profile_processing\"\n")
	findings := ScanPath(root, nil)
	assertHasCode(t, findings, "UPD102")
}

func TestNestedApplicationClassifierUsesNearestScope(t *testing.T) {
	module := ClassifyPath("apps/product/ui/commander/applications/settings/data/screen.go")
	if module.ApplicationID != "settings" || module.Layer != "data" || module.Role != "" {
		t.Fatalf("unexpected path classification: %+v", module)
	}
	dependency := ClassifyImport("example/apps/product/ui/commander/applications/profile/process/profile_processing")
	if dependency.ApplicationID != "profile" || dependency.Layer != "process" || dependency.Role != "processing" {
		t.Fatalf("unexpected import classification: %+v", dependency)
	}
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

func TestUPD203ResolvesImportAlias(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "process", "alias_commander.go")
	writeTestFile(t, path, "package process\nimport nethttp \"net/http\"\nfunc run() { _, _ = nethttp.Get(\"https://example.com\") }\n")
	findings := ScanPath(root, nil)
	assertHasCode(t, findings, "UPD203")
}

func TestUPD203IgnoresSameNamedLocalValue(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "process", "local_commander.go")
	writeTestFile(t, path, "package process\ntype fakeOS struct{}\nfunc (fakeOS) Open(string) {}\nfunc run() { os := fakeOS{}; os.Open(\"sample.txt\") }\n")
	findings := ScanPath(root, nil)
	assertNoCode(t, findings, "UPD203")
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
