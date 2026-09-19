package checker

import (
	"path/filepath"
	"testing"
)

func TestSharedProcessingDoesNotBypassApplicationBoundary(t *testing.T) {
	findings := scanSharedBoundaryTarget(t, "shared/process/settings_processing")
	assertHasCode(codeAssertionInput{t: t, findings: findings, code: "UPD102"})
}

func TestSharedDataDoesNotBypassApplicationBoundary(t *testing.T) {
	findings := scanSharedBoundaryTarget(t, "shared/data/storage")
	assertHasCode(codeAssertionInput{t: t, findings: findings, code: "UPD102"})
}

func TestSharedContractsRemainBoundaryAPI(t *testing.T) {
	findings := scanSharedBoundaryTarget(t, "shared/contracts/process/settings_processing")
	assertNoCode(codeAssertionInput{t: t, findings: findings, code: "UPD102"})
}

func TestSharedMessagesRemainBoundaryAPI(t *testing.T) {
	findings := scanSharedBoundaryTarget(t, "shared/messages/process/settings_processing")
	assertNoCode(codeAssertionInput{t: t, findings: findings, code: "UPD102"})
}

func scanSharedBoundaryTarget(t *testing.T, targetSuffix string) []Finding {
	t.Helper()
	root := t.TempDir()
	targetPackage := filepath.ToSlash(filepath.Join("applications", "settings", targetSuffix))
	source := filepath.Join(root, "applications", "main", "process", "main_commander.go")
	writeTestFile(t, source, "package process\nimport _ \"example/"+targetPackage+"\"\n")
	target := filepath.Join(root, filepath.FromSlash(targetPackage), "target.go")
	writeTestFile(t, target, "package target\n")
	return ScanPath(root, nil)
}
