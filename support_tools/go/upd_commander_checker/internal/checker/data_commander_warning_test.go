package checker

import "testing"

func TestDataCommanderToDataCommanderWarning(t *testing.T) {
	source := ClassifyPath("data/save_commander.go")
	target := ClassifyImport("example/data/cache_commander")

	result := DependencyResult(source, target)
	if result == nil {
		t.Fatal("expected Data Commander warning result")
	}
	if result.Code != "UPD103" {
		t.Fatalf("expected UPD103, got %s", result.Code)
	}
	if result.Severity != "warning" {
		t.Fatalf("expected warning severity, got %s", result.Severity)
	}
}
