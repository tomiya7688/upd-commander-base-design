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

func TestSameNameLocalVariableDoesNotPromote(t *testing.T) {
	root := t.TempDir()
	writeTestFile(t, filepath.Join(root, "models.go"), "package sample\ntype First struct{ Value int }\ntype Second struct{ Value int }\n")
	writeTestFile(t, filepath.Join(root, "consumer.go"), "package sample\nfunc use() int { First := 1; return First }\n")
	findings := ScanPath(root, nil)
	if hasFinding(findings, "UPD404", "warning") {
		t.Fatalf("same-name local variable must not promote data type: %#v", findings)
	}
}

func TestOnlyReferencedSiblingTypeIsPromoted(t *testing.T) {
	root := t.TempDir()
	writeTestFile(t, filepath.Join(root, "models.go"), "package sample\ntype First struct{ Value int }\ntype Second struct{ Value int }\n")
	writeTestFile(t, filepath.Join(root, "consumer.go"), "package sample\nvar value Second\n")
	findings := ScanPath(root, nil)
	firstCode := ""
	secondCode := ""
	for _, finding := range findings {
		if finding.Code != "UPD403" && finding.Code != "UPD404" {
			continue
		}
		if finding.Message == "data-only type First shares a file with another type" {
			firstCode = finding.Code
		}
		if finding.Message == "data-only type Second shares a file with another type and is referenced from another file" {
			secondCode = finding.Code
		}
	}
	if firstCode != "UPD403" || secondCode != "UPD404" {
		t.Fatalf("expected First=UPD403 and Second=UPD404, got %#v", findings)
	}
}

func TestImportedPackageSelectorPromotesExactType(t *testing.T) {
	root := t.TempDir()
	writeTestFile(t, filepath.Join(root, "go.mod"), "module example.com/sample\n\ngo 1.23\n")
	writeTestFile(t, filepath.Join(root, "models", "models.go"), "package models\ntype First struct{ Value int }\ntype Second struct{ Value int }\n")
	writeTestFile(t, filepath.Join(root, "consumer", "consumer.go"), "package consumer\nimport modeltypes \"example.com/sample/models\"\nvar value modeltypes.First\n")
	findings := ScanPath(root, nil)
	firstWarning := false
	secondWarning := false
	for _, finding := range findings {
		if finding.Code != "UPD404" {
			continue
		}
		if finding.Message == "data-only type First shares a file with another type and is referenced from another file" {
			firstWarning = true
		}
		if finding.Message == "data-only type Second shares a file with another type and is referenced from another file" {
			secondWarning = true
		}
	}
	if !firstWarning || secondWarning {
		t.Fatalf("expected only First to be promoted, got %#v", findings)
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
