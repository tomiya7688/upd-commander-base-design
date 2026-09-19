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
	content := `{"input":"project","output":"reports/check.txt","ignore":["generated/**"],"warnings_as_errors":true,"enabled_rules":["UPD101","UPD202"],"upd301_max_inputs":3,"flat_layer_min_files":14,"flat_layer_min_direct_percent":90,"model_group_min_items":4,"model_group_min_occurrences":3}`
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
	if config.EnabledRules == nil || len(*config.EnabledRules) != 2 || (*config.EnabledRules)[0] != "UPD101" {
		t.Fatalf("unexpected enabled rules: %#v", config.EnabledRules)
	}
	if config.Upd301MaxInputs != 3 {
		t.Fatalf("unexpected upd301 max inputs: %d", config.Upd301MaxInputs)
	}
	if config.FlatLayerMinFiles != 14 || config.FlatLayerMinDirectPercent != 90 {
		t.Fatalf("unexpected flat layer thresholds: %d/%d", config.FlatLayerMinFiles, config.FlatLayerMinDirectPercent)
	}
	if config.ModelGroupMinItems != 4 || config.ModelGroupMinOccurrences != 3 {
		t.Fatalf("unexpected model thresholds: %d/%d", config.ModelGroupMinItems, config.ModelGroupMinOccurrences)
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

func TestNullEnabledRulesReturnsError(t *testing.T) {
	original, _ := os.Getwd()
	root := t.TempDir()
	configDir := filepath.Join(root, "config")
	if err := os.MkdirAll(configDir, 0o755); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(filepath.Join(configDir, "path.json"), []byte(`{"enabled_rules":null}`), 0o644); err != nil {
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

func TestMissingUpd301MaxInputsUsesDefault(t *testing.T) {
	original, _ := os.Getwd()
	root := t.TempDir()
	configDir := filepath.Join(root, "config")
	if err := os.MkdirAll(configDir, 0o755); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(filepath.Join(configDir, "path.json"), []byte(`{}`), 0o644); err != nil {
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
	if config.Upd301MaxInputs != 2 {
		t.Fatalf("unexpected default: %d", config.Upd301MaxInputs)
	}
	if config.FlatLayerMinFiles != 12 || config.FlatLayerMinDirectPercent != 80 {
		t.Fatalf("unexpected flat layer defaults: %d/%d", config.FlatLayerMinFiles, config.FlatLayerMinDirectPercent)
	}
	if config.ModelGroupMinItems != 3 || config.ModelGroupMinOccurrences != 2 {
		t.Fatalf("unexpected model defaults: %d/%d", config.ModelGroupMinItems, config.ModelGroupMinOccurrences)
	}
}

func TestInvalidFlatLayerThresholdsReturnError(t *testing.T) {
	cases := []string{
		`{"flat_layer_min_files":0}`,
		`{"flat_layer_min_files":true}`,
		`{"flat_layer_min_direct_percent":0}`,
		`{"flat_layer_min_direct_percent":101}`,
		`{"flat_layer_min_direct_percent":80.0}`,
	}
	for _, content := range cases {
		t.Run(content, func(t *testing.T) {
			original, _ := os.Getwd()
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
			defer func() { _ = os.Chdir(original) }()
			if _, err := LoadConfig(); err == nil {
				t.Fatal("expected config error")
			}
		})
	}
}
