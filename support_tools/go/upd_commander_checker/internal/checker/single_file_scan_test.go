package checker

import (
	"path/filepath"
	"testing"
)

func TestSingleFileScanPreservesUILayer(t *testing.T) {
	root := t.TempDir()
	target := filepath.Join(root, "applications", "main", "ui", "screen.go")
	writeTestFile(
		t,
		target,
		"package ui\nimport _ \"example/applications/main/data/storage\"\n",
	)
	writeTestFile(
		t,
		filepath.Join(root, "applications", "main", "data", "storage", "storage.go"),
		"package storage\n",
	)

	findings := ScanPath(target, nil)
	assertHasCode(codeAssertionInput{t: t, findings: findings, code: "UPD101"})
}

func TestSingleFileScanPreservesApplicationBoundary(t *testing.T) {
	root := t.TempDir()
	target := filepath.Join(root, "applications", "main", "process", "main_commander.go")
	writeTestFile(
		t,
		target,
		"package process\nimport _ \"example/applications/settings/process/settings_processing\"\n",
	)
	writeTestFile(
		t,
		filepath.Join(
			root,
			"applications",
			"settings",
			"process",
			"settings_processing",
			"processing.go",
		),
		"package settings_processing\n",
	)

	findings := ScanPath(target, nil)
	assertHasCode(codeAssertionInput{t: t, findings: findings, code: "UPD102"})
}
