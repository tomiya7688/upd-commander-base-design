package checker

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

func TestResponsibilityWarning(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "large.go")
	content := "package sample\n" + strings.Repeat("var value = 1\n", 360)
	if err := os.WriteFile(path, []byte(content), 0o644); err != nil {
		t.Fatal(err)
	}
	findings := ScanPath(root, nil)
	for _, finding := range findings {
		if finding.Code == "UPD401" {
			return
		}
	}
	t.Fatal("expected UPD401")
}
