package checker

import (
	"encoding/json"
	"os"
	"path/filepath"
)

type Config struct {
	Input            string   `json:"input"`
	Output           string   `json:"output"`
	Ignore           []string `json:"ignore"`
	WarningsAsErrors bool     `json:"warnings_as_errors"`
}

func LoadConfig() Config {
	config := Config{Input: "."}
	path := findConfigPath()
	if path == "" {
		return config
	}
	data, err := os.ReadFile(path)
	if err != nil || json.Unmarshal(data, &config) != nil {
		return Config{Input: "."}
	}
	if config.Input == "" {
		config.Input = "."
	}
	base := filepath.Dir(filepath.Dir(path))
	config.Input = resolveConfigPath(base, config.Input)
	if config.Output != "" {
		config.Output = resolveConfigPath(base, config.Output)
	}
	return config
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
