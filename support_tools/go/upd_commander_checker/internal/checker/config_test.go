package checker

import (
	"os"
	"path/filepath"
	"testing"
)

func TestLoadConfigFromCurrentDirectory(t *testing.T) {
	original, err := os.Getwd()
	if err != nil {
		t.Fatal(err)
	}
	root := t.TempDir()
	configDir := filepath.Join(root, "config")
	if err := os.MkdirAll(configDir, 0o755); err != nil {
		t.Fatal(err)
	}
	content := `{"input":"project","output":"reports/check.txt","ignore":["generated/**"],"warnings_as_errors":true}`
	if err := os.WriteFile(filepath.Join(configDir, "path.json"), []byte(content), 0o644); err != nil {
		t.Fatal(err)
	}
	if err := os.Chdir(root); err != nil {
		t.Fatal(err)
	}
	defer func() { _ = os.Chdir(original) }()

	config, err := LoadConfig()
	if err != nil {
		t.Fatal(err)
	}
	if config.Input != filepath.Join(root, "project") {
		t.Fatalf("unexpected input: %s", config.Input)
	}
	if config.Output != filepath.Join(root, "reports", "check.txt") {
		t.Fatalf("unexpected output: %s", config.Output)
	}
	if len(config.Ignore) != 1 || config.Ignore[0] != "generated/**" {
		t.Fatalf("unexpected ignore: %#v", config.Ignore)
	}
	if !config.WarningsAsErrors {
		t.Fatal("warnings_as_errors was not loaded")
	}
}

func TestInvalidConfigReturnsError(t *testing.T) {
	original, _ := os.Getwd()
	root := t.TempDir()
	configDir := filepath.Join(root, "config")
	if err := os.MkdirAll(configDir, 0o755); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(filepath.Join(configDir, "path.json"), []byte(`{"ignore":[1]}`), 0o644); err != nil {
		t.Fatal(err)
	}
	if err := os.Chdir(root); err != nil {
		t.Fatal(err)
	}
	defer func() { _ = os.Chdir(original) }()

	if _, err := LoadConfig(); err == nil {
		t.Fatal("expected config error")
	}
}
