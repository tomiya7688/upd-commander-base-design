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
	writeTestFile(t, filepath.Join(root, "data", "storage", "storage.go"), "package storage\n")
	findings := ScanPath(root, nil)
	assertHasCode(codeAssertionInput{t: t, findings: findings, code: "UPD101"})
}

func TestCrossApplicationInternalImportIsReported(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "applications", "main", "process", "main_commander.go")
	writeTestFile(t, path, "package process\nimport _ \"example/applications/settings/process/settings_processing\"\n")
	writeTestFile(t, filepath.Join(root, "applications", "settings", "process", "settings_processing", "processing.go"), "package settings_processing\n")
	findings := ScanPath(root, nil)
	assertHasCode(codeAssertionInput{t: t, findings: findings, code: "UPD102"})
}

func TestNestedApplicationInternalImportIsReported(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "apps", "product", "applications", "settings", "ui", "screen_processing.go")
	writeTestFile(t, path, "package ui\nimport _ \"example/apps/product/applications/profile/process/profile_processing\"\n")
	writeTestFile(t, filepath.Join(root, "apps", "product", "applications", "profile", "process", "profile_processing", "processing.go"), "package profile_processing\n")
	findings := ScanPath(root, nil)
	assertHasCode(codeAssertionInput{t: t, findings: findings, code: "UPD102"})
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

func TestCommonClassifierUsesDefaultsAndCustomRoots(t *testing.T) {
	common := ClassifyPath("applications/main/common/contracts/message.go")
	if common.ApplicationID != "main" || common.Layer != "common" {
		t.Fatalf("unexpected common classification: %+v", common)
	}
	shared := ClassifyImport("example/applications/main/shared/contracts/message")
	if shared.ApplicationID != "main" || shared.Layer != "common" {
		t.Fatalf("unexpected shared classification: %+v", shared)
	}
	custom := ClassifyPathWithCommonRoots("applications/main/contracts/message.go", []string{"contracts"})
	if custom.Layer != "common" {
		t.Fatalf("unexpected custom common classification: %+v", custom)
	}
	disabled := ClassifyPathWithCommonRoots("common/message.go", []string{})
	if disabled.Layer != "" {
		t.Fatalf("common recognition should be disabled: %+v", disabled)
	}
	innerLayer := ClassifyPath("common/ui/screen.go")
	if innerLayer.Layer != "ui" {
		t.Fatalf("innermost layer should win: %+v", innerLayer)
	}
	innerCommon := ClassifyPath("ui/common/message.go")
	if innerCommon.Layer != "common" {
		t.Fatalf("innermost common should win: %+v", innerCommon)
	}
}

func TestCommonDependenciesFollowNeutralityRules(t *testing.T) {
	t.Run("layer to common is allowed", func(t *testing.T) {
		root := t.TempDir()
		writeTestFile(
			t,
			filepath.Join(root, "ui", "screen.go"),
			"package ui\nimport _ \"example/common/contracts\"\n",
		)
		writeTestFile(
			t,
			filepath.Join(root, "common", "contracts", "contracts.go"),
			"package contracts\n",
		)
		assertNoCode(
			codeAssertionInput{t: t, findings: ScanPath(root, nil), code: "UPD101"},
		)
	})

	t.Run("common to layer is rejected", func(t *testing.T) {
		root := t.TempDir()
		writeTestFile(
			t,
			filepath.Join(root, "common", "helper.go"),
			"package common\nimport _ \"example/process/work\"\n",
		)
		writeTestFile(
			t,
			filepath.Join(root, "process", "work", "work.go"),
			"package work\n",
		)
		assertHasCode(
			codeAssertionInput{t: t, findings: ScanPath(root, nil), code: "UPD101"},
		)
	})

	t.Run("application local common keeps application boundary", func(t *testing.T) {
		root := t.TempDir()
		writeTestFile(
			t,
			filepath.Join(root, "applications", "main", "process", "run.go"),
			"package process\nimport _ \"example/applications/settings/common/internal\"\n",
		)
		writeTestFile(
			t,
			filepath.Join(root, "applications", "settings", "common", "internal", "internal.go"),
			"package internal\n",
		)
		assertHasCode(
			codeAssertionInput{t: t, findings: ScanPath(root, nil), code: "UPD102"},
		)
	})
}

func TestConfiguredCommonRootsAreUsedDuringScan(t *testing.T) {
	root := t.TempDir()
	writeTestFile(
		t,
		filepath.Join(root, "contracts", "helper.go"),
		"package contracts\nimport _ \"example/process/work\"\n",
	)
	writeTestFile(
		t,
		filepath.Join(root, "process", "work", "work.go"),
		"package work\n",
	)

	defaultFindings := ScanPath(root, nil)
	assertNoCode(codeAssertionInput{t: t, findings: defaultFindings, code: "UPD101"})

	options := DefaultScanOptions()
	options.CommonRoots = []string{"contracts"}
	configuredFindings := ScanPathWithOptions(root, nil, options)
	assertHasCode(codeAssertionInput{t: t, findings: configuredFindings, code: "UPD101"})
}

func TestCrossApplicationMessengerIsAllowed(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "applications", "main", "process", "main_commander.go")
	writeTestFile(t, path, "package process\nimport _ \"example/applications/settings/process/settings_messenger\"\n")
	writeTestFile(t, filepath.Join(root, "applications", "settings", "process", "settings_messenger", "messenger.go"), "package settings_messenger\n")
	findings := ScanPath(root, nil)
	assertNoCode(codeAssertionInput{t: t, findings: findings, code: "UPD102"})
}

func TestBoundaryLikeDirectoryIsNotBoundaryAPI(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "applications", "main", "process", "main_commander.go")
	writeTestFile(t, path, "package process\nimport _ \"example/applications/settings/contractor/process/settings_processing\"\n")
	writeTestFile(t, filepath.Join(root, "applications", "settings", "contractor", "process", "settings_processing", "processing.go"), "package settings_processing\n")
	findings := ScanPath(root, nil)
	assertHasCode(codeAssertionInput{t: t, findings: findings, code: "UPD102"})
}

func TestExplicitBoundaryAPILayerViolationKeepsUPD101(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "applications", "main", "ui", "screen_processing.go")
	writeTestFile(t, path, "package ui\nimport _ \"example/applications/settings/contracts/data/storage\"\n")
	writeTestFile(t, filepath.Join(root, "applications", "settings", "contracts", "data", "storage", "storage.go"), "package storage\n")
	findings := ScanPath(root, nil)
	assertHasCode(codeAssertionInput{t: t, findings: findings, code: "UPD101"})
	assertNoCode(codeAssertionInput{t: t, findings: findings, code: "UPD102"})
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
	assertNoCode(codeAssertionInput{t: t, findings: findings, code: "UPD202"})
}

func TestUPD203ResolvesImportAlias(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "process", "alias_commander.go")
	writeTestFile(t, path, "package process\nimport nethttp \"net/http\"\nfunc run() { _, _ = nethttp.Get(\"https://example.com\") }\n")
	findings := ScanPath(root, nil)
	assertHasCode(codeAssertionInput{t: t, findings: findings, code: "UPD203"})
}

func TestUPD203IgnoresSameNamedLocalValue(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "process", "local_commander.go")
	writeTestFile(t, path, "package process\ntype fakeOS struct{}\nfunc (fakeOS) Open(string) {}\nfunc run() { os := fakeOS{}; os.Open(\"sample.txt\") }\n")
	findings := ScanPath(root, nil)
	assertNoCode(codeAssertionInput{t: t, findings: findings, code: "UPD203"})
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

type codeAssertionInput struct {
	t        *testing.T
	findings []Finding
	code     string
}

func assertHasCode(input codeAssertionInput) {
	input.t.Helper()
	for _, finding := range input.findings {
		if finding.Code == input.code {
			return
		}
	}
	input.t.Fatalf("missing %s: %+v", input.code, input.findings)
}

func assertNoCode(input codeAssertionInput) {
	input.t.Helper()
	for _, finding := range input.findings {
		if finding.Code == input.code {
			input.t.Fatalf("unexpected %s: %+v", input.code, finding)
		}
	}
}
