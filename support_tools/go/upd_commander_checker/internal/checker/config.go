package checker

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
)

type Config struct {
	Input            string   `json:"input"`
	Output           string   `json:"output"`
	Ignore           []string `json:"ignore"`
	WarningsAsErrors bool     `json:"warnings_as_errors"`
}

func LoadConfig() (Config, error) {
	config := Config{Input: "."}
	path := findConfigPath()
	if path == "" {
		return config, nil
	}
	data, err := os.ReadFile(path)
	if err != nil {
		return Config{}, fmt.Errorf("invalid config: %s", path)
	}
	var raw map[string]json.RawMessage
	if err := json.Unmarshal(data, &raw); err != nil {
		return Config{}, fmt.Errorf("invalid config: %s", path)
	}
	if value, ok := raw["input"]; ok && json.Unmarshal(value, &config.Input) != nil {
		return Config{}, fmt.Errorf("invalid config field: input")
	}
	if value, ok := raw["output"]; ok && json.Unmarshal(value, &config.Output) != nil {
		return Config{}, fmt.Errorf("invalid config field: output")
	}
	if value, ok := raw["ignore"]; ok && json.Unmarshal(value, &config.Ignore) != nil {
		return Config{}, fmt.Errorf("invalid config field: ignore")
	}
	if value, ok := raw["warnings_as_errors"]; ok && json.Unmarshal(value, &config.WarningsAsErrors) != nil {
		return Config{}, fmt.Errorf("invalid config field: warnings_as_errors")
	}
	if config.Input == "" {
		config.Input = "."
	}
	base := filepath.Dir(filepath.Dir(path))
	config.Input = resolveConfigPath(base, config.Input)
	if config.Output != "" {
		config.Output = resolveConfigPath(base, config.Output)
	}
	return config, nil
}

func findConfigPath() string {
	candidates := []string{}
	if executable, err := os.Executable(); err == nil {
		candidates = append(candidates, filepath.Join(filepath.Dir(executable), "config", "path.json"))
	}
	if cwd, err := os.Getwd(); err == nil {
		candidates = append(candidates, filepath.Join(cwd, "config", "path.json"))
	}
	for _, path := range candidates {
		if info, err := os.Stat(path); err == nil && !info.IsDir() {
			return path
		}
	}
	return ""
}

func resolveConfigPath(base string, value string) string {
	if filepath.IsAbs(value) {
		return value
	}
	absolute, err := filepath.Abs(filepath.Join(base, value))
	if err != nil {
		return filepath.Join(base, value)
	}
	return absolute
}
