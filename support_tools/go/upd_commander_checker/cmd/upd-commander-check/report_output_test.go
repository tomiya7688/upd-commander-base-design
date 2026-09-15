package main

import (
	"os"
	"path/filepath"
	"testing"
)

func TestFinishReportParentCreationFailure(t *testing.T) {
	root := t.TempDir()
	blocker := filepath.Join(root, "blocker")
	if err := os.WriteFile(blocker, []byte("file"), 0o644); err != nil {
		t.Fatal(err)
	}
	output := filepath.Join(blocker, "report.txt")
	if code := finishReport([]string{"OK"}, output, 0); code != 2 {
		t.Fatalf("expected output failure exit code 2, got %d", code)
	}
}

func TestFinishReportDirectoryOutputFailure(t *testing.T) {
	output := filepath.Join(t.TempDir(), "report")
	if err := os.Mkdir(output, 0o755); err != nil {
		t.Fatal(err)
	}
	if code := finishReport([]string{"FAIL e=1 w=0 a=0"}, output, 1); code != 2 {
		t.Fatalf("expected output failure exit code 2, got %d", code)
	}
}
