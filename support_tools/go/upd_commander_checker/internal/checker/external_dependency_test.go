package checker

import (
	"path/filepath"
	"testing"
)

func TestExternalDataPackageDoesNotTriggerLayerRule(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "ui", "screen.go")
	writeTestFile(t, path, "package ui\nimport _ \"thirdparty/data/client\"\n")

	findings := ScanPath(root, nil)
	assertNoCode(t, findings, "UPD101")
}

func TestExternalProcessingPackageDoesNotTriggerRoleRule(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "process", "event_messenger.go")
	writeTestFile(t, path, "package process\nimport _ \"vendor/processing/engine\"\n")

	findings := ScanPath(root, nil)
	assertNoCode(t, findings, "UPD101")
}

func TestMissingOtherApplicationPackageIsNotInternal(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "applications", "main", "process", "main_commander.go")
	writeTestFile(t, path, "package process\nimport _ \"example/applications/external/data/client\"\n")

	findings := ScanPath(root, nil)
	assertNoCode(t, findings, "UPD102")
}
