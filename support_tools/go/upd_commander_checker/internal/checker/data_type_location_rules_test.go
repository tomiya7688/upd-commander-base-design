package checker

import (
	"os"
	"path/filepath"
	"testing"
)

func TestLocalDataTypesAreAttention(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "models.go")
	if err := os.WriteFile(path, []byte("package sample\ntype First struct { Value int }\ntype Second struct { Value int }\n"), 0o644); err != nil {
		t.Fatal(err)
	}
	findings := ScanPath(root, nil)
	if !hasFinding(findings, "UPD403", "attention") {
		t.Fatalf("expected UPD403 attention, got %#v", findings)
	}
	if hasFinding(findings, "UPD404", "warning") {
		t.Fatalf("did not expect UPD404, got %#v", findings)
	}
}

func TestExternalDataTypeReferenceIsWarning(t *testing.T) {
	root := t.TempDir()
	models := filepath.Join(root, "models.go")
	consumer := filepath.Join(root, "consumer.go")
	if err := os.WriteFile(models, []byte("package sample\ntype First struct { Value int }\ntype Second struct { Value int }\n"), 0o644); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(consumer, []byte("package sample\nvar value First\n"), 0o644); err != nil {
		t.Fatal(err)
	}
	findings := ScanPath(root, nil)
	if !hasFinding(findings, "UPD404", "warning") {
		t.Fatalf("expected UPD404 warning, got %#v", findings)
	}
}

func hasFinding(findings []Finding, code string, severity string) bool {
	for _, finding := range findings {
		if finding.Code == code && finding.Severity == severity {
			return true
		}
	}
	return false
}
