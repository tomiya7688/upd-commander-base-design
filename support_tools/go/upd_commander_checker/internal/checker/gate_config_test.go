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
		`{"fail_on_scope":null}`, `{"fail_on_scope":"existing"}`,
		`{"severity_overrides":null}`, `{"severity_overrides":[]}`,
		`{"severity_overrides":{"bad":"error"}}`, `{"severity_overrides":{"UPD101":"fatal"}}`,
		`{"gate_exceptions":null}`,
		`{"gate_exceptions":[{"rule":"UPD203","path":"../a.go","reason":"bad"}]}`,
		`{"gate_exceptions":[{"rule":"UPD203","path":"a.go","reason":""}]}`,
		`{"gate_exceptions":[{"rule":"UPD203","path":"a.go","reason":"ok","extra":1}]}`,
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

func TestFailOnScopeDefaultsAndLoads(t *testing.T) {
	if err := writeGateConfigAndChdir(t, `{}`); err != nil {
		t.Fatal(err)
	}
	config, err := LoadConfig()
	if err != nil || config.FailOnScope != "all" {
		t.Fatalf("default fail_on_scope not loaded: %q, %v", config.FailOnScope, err)
	}
	if err := writeGateConfigAndChdir(t, `{"fail_on_scope":"new"}`); err != nil {
		t.Fatal(err)
	}
	config, err = LoadConfig()
	if err != nil || config.FailOnScope != "new" {
		t.Fatalf("new fail_on_scope not loaded: %q, %v", config.FailOnScope, err)
	}
}

func TestLoadsGateExceptions(t *testing.T) {
	if err := writeGateConfigAndChdir(t, `{"gate_exceptions":[{"rule":"upd203","path":"src/a.go","line":8,"reason":"approved"}]}`); err != nil {
		t.Fatal(err)
	}
	config, err := LoadConfig()
	if err != nil || len(config.GateExceptions) != 1 || config.GateExceptions[0].Rule != "UPD203" || config.GateExceptions[0].Line == nil || *config.GateExceptions[0].Line != 8 {
		t.Fatalf("gate exceptions not loaded: %#v, %v", config.GateExceptions, err)
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
