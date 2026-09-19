package checker

import (
	"os"
	"path/filepath"
	"testing"
)

func TestMissingIgnoreFileIsNormal(t *testing.T) {
	root := t.TempDir()
	writeTestFile(t, filepath.Join(root, "plain.go"), "package plain\n")

	findings := ScanPath(root, nil)
	assertNoCode(codeAssertionInput{t: t, findings: findings, code: "UPD001"})
}

func TestUnreadableIgnoreFileReportsUPD001AndStopsScan(t *testing.T) {
	root := t.TempDir()
	ignorePath := filepath.Join(root, ".updcommanderignore")
	writeTestFile(t, ignorePath, "generated/**\n")
	writeTestFile(t, filepath.Join(root, "broken.go"), "package broken\nfunc broken(\n")
	if err := os.Chmod(ignorePath, 0); err != nil {
		t.Fatal(err)
	}
	defer os.Chmod(ignorePath, 0o600)
	if _, err := os.ReadFile(ignorePath); err == nil {
		t.Skip("current user bypasses file permissions")
	}

	findings := ScanPath(root, nil)
	assertHasCode(codeAssertionInput{t: t, findings: findings, code: "UPD001"})
	assertNoCode(codeAssertionInput{t: t, findings: findings, code: "UPD002"})
}

func TestUnreadableDirectoryReportsUPD001AndContinues(t *testing.T) {
	root := t.TempDir()
	blocked := filepath.Join(root, "blocked")
	writeTestFile(t, filepath.Join(blocked, "hidden.go"), "package blocked\n")
	writeTestFile(
		t,
		filepath.Join(root, "process", "loop_commander.go"),
		"package process\nfunc LoopCommander() { for i := 0; i < 3; i++ {} }\n",
	)
	if err := os.Chmod(blocked, 0); err != nil {
		t.Fatal(err)
	}
	defer os.Chmod(blocked, 0o700)
	if _, err := os.ReadDir(blocked); err == nil {
		t.Skip("current user bypasses directory permissions")
	}

	findings := ScanPath(root, nil)
	assertHasCode(codeAssertionInput{t: t, findings: findings, code: "UPD001"})
	assertHasCode(codeAssertionInput{t: t, findings: findings, code: "UPD201"})
}
