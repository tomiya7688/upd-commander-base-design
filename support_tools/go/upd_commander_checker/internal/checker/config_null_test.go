package checker

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

func TestConfigRejectsNullAndInvalidTypedFields(t *testing.T) {
	cases := []struct {
		name    string
		content string
		field   string
	}{
		{"null input", `{"input":null}`, "input"},
		{"null output", `{"output":null}`, "output"},
		{"null ignore", `{"ignore":null}`, "ignore"},
		{"null ignore item", `{"ignore":[null]}`, "ignore"},
		{"null warnings", `{"warnings_as_errors":null}`, "warnings_as_errors"},
		{"null enabled rules", `{"enabled_rules":null}`, "enabled_rules"},
		{"number input", `{"input":1}`, "input"},
		{"string warnings", `{"warnings_as_errors":"true"}`, "warnings_as_errors"},
	}

	for _, testCase := range cases {
		t.Run(testCase.name, func(t *testing.T) {
			error := loadConfigErrorFromContent(t, testCase.content)
			if error == nil {
				t.Fatal("expected config error")
			}
			if !strings.Contains(error.Error(), "invalid config field: "+testCase.field) {
				t.Fatalf("unexpected error: %v", error)
			}
		})
	}
}

func TestConfigRejectsNullRoot(t *testing.T) {
	error := loadConfigErrorFromContent(t, "null")
	if error == nil || !strings.Contains(error.Error(), "invalid config:") {
		t.Fatalf("unexpected error: %v", error)
	}
}

func loadConfigErrorFromContent(t *testing.T, content string) error {
	t.Helper()
	original, err := os.Getwd()
	if err != nil {
		t.Fatal(err)
	}
	root := t.TempDir()
	configDir := filepath.Join(root, "config")
	if err := os.MkdirAll(configDir, 0o755); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(filepath.Join(configDir, "path.json"), []byte(content), 0o644); err != nil {
		t.Fatal(err)
	}
	if err := os.Chdir(root); err != nil {
		t.Fatal(err)
	}
	defer func() {
		if err := os.Chdir(original); err != nil {
			t.Errorf("restore working directory: %v", err)
		}
	}()

	_, loadErr := LoadConfig()
	return loadErr
}
