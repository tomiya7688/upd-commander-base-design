package checker

import "testing"

func TestDataCommanderToDataCommanderWarning(t *testing.T) {
	source := ClassifyPath("data/save_commander.go")
	target := ClassifyImport("example/data/cache_commander")

	if message := DependencyError(source, target); message != "" {
		t.Fatalf("unexpected dependency error: %s", message)
	}
	if message := DataCommanderWarning(source, target); message == "" {
		t.Fatal("expected Data Commander warning")
	}
}
