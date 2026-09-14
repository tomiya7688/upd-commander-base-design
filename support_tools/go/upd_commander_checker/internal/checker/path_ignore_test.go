package checker

import (
	"path/filepath"
	"testing"
)

func TestPathOnlyIgnoreSkipsBrokenFileBeforeParsing(t *testing.T) {
	root := t.TempDir()
	writeTestFile(t, filepath.Join(root, ".updcommanderignore"), "generated/**\n")
	writeTestFile(t, filepath.Join(root, "generated", "broken.go"), "package generated\nfunc broken(\n")

	findings := ScanPath(root, nil)
	assertNoCode(t, findings, "UPD002")
}

func TestRuleSpecificIgnoreDoesNotSkipBrokenFile(t *testing.T) {
	root := t.TempDir()
	writeTestFile(t, filepath.Join(root, ".updcommanderignore"), "UPD202 generated/**\n")
	writeTestFile(t, filepath.Join(root, "generated", "broken.go"), "package generated\nfunc broken(\n")

	findings := ScanPath(root, nil)
	assertHasCode(t, findings, "UPD002")
}
