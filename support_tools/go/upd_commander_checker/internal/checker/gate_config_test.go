package checker

import (
	"os"
	"path/filepath"
	"testing"
)

func TestExplicitEmptyFailOnIsPreserved(t *testing.T) {
	if err := writeGateConfigAndChdir(t, `{"fail_on":[]}`); err != nil {
		t.Fatal(err)
	}
	config, err := LoadConfig()
	if err != nil || config.FailOn == nil || len(*config.FailOn) != 0 {
		t.Fatalf("explicit empty fail_on was not preserved: %#v, %v", config.FailOn, err)
	}
}

func TestInvalidGateConfigReturnsError(t *testing.T) {
	for _, content := range []string{
		`{"fail_on":null}`, `{"fail_on":"error"}`, `{"fail_on":["fatal"]}`,
		`{"severity_overrides":null}`, `{"severity_overrides":[]}`,
		`{"severity_overrides":{"bad":"error"}}`, `{"severity_overrides":{"UPD101":"fatal"}}`,
	} {
		t.Run(content, func(t *testing.T) {
			if err := writeGateConfigAndChdir(t, content); err != nil {
				t.Fatal(err)
			}
			if _, err := LoadConfig(); err == nil {
				t.Fatal("expected config error")
			}
		})
	}
}

func writeGateConfigAndChdir(t *testing.T, content string) error {
	t.Helper()
	original, err := os.Getwd()
	if err != nil {
		return err
	}
	root := t.TempDir()
	configDir := filepath.Join(root, "config")
	if err := os.MkdirAll(configDir, 0o755); err != nil {
		return err
	}
	if err := os.WriteFile(filepath.Join(configDir, "path.json"), []byte(content), 0o644); err != nil {
		return err
	}
	if err := os.Chdir(root); err != nil {
		return err
	}
	t.Cleanup(func() { _ = os.Chdir(original) })
	return nil
}
