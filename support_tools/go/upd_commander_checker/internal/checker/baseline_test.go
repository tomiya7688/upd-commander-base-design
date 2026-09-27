package checker

import (
	"encoding/json"
	"os"
	"path/filepath"
	"runtime"
	"testing"
)

type baselineFixtureFile struct {
	Vectors []struct {
		Name    string `json:"name"`
		Finding struct {
			Rule    string `json:"rule"`
			Path    string `json:"path"`
			Symbol  string `json:"symbol"`
			Context string `json:"context"`
		} `json:"finding"`
		ExpectedFingerprint string `json:"expected_fingerprint"`
	} `json:"vectors"`
	Comparisons []struct {
		Left          string `json:"left"`
		Right         string `json:"right"`
		ExpectedEqual bool   `json:"expected_equal"`
	} `json:"comparisons"`
}

func TestFindingFingerprintGoldenVectors(t *testing.T) {
	_, sourceFile, _, ok := runtime.Caller(0)
	if !ok {
		t.Fatal("cannot locate test source")
	}
	fixturePath := filepath.Join(filepath.Dir(sourceFile), "..", "..", "..", "..", "..", "specification", "baseline-fingerprint-fixtures.json")
	data, err := os.ReadFile(filepath.Clean(fixturePath))
	if err != nil {
		t.Fatalf("read golden fixture: %v", err)
	}
	var fixture baselineFixtureFile
	if err := json.Unmarshal(data, &fixture); err != nil {
		t.Fatalf("decode golden fixture: %v", err)
	}
	fingerprints := make(map[string]string, len(fixture.Vectors))
	for _, vector := range fixture.Vectors {
		actual, err := FindingFingerprint(vector.Finding.Rule, vector.Finding.Path, vector.Finding.Symbol, vector.Finding.Context)
		if err != nil {
			t.Errorf("%s: fingerprint error: %v", vector.Name, err)
			continue
		}
		if actual != vector.ExpectedFingerprint {
			t.Errorf("%s: expected %s, got %s", vector.Name, vector.ExpectedFingerprint, actual)
		}
		fingerprints[vector.Name] = actual
	}
	for _, comparison := range fixture.Comparisons {
		left, leftOK := fingerprints[comparison.Left]
		right, rightOK := fingerprints[comparison.Right]
		if !leftOK || !rightOK {
			t.Errorf("fixture comparison references unknown vector: %s vs %s", comparison.Left, comparison.Right)
			continue
		}
		if (left == right) != comparison.ExpectedEqual {
			t.Errorf("unexpected equality for %s vs %s", comparison.Left, comparison.Right)
		}
	}
}

func TestFindingFingerprintRejectsInvalidIdentity(t *testing.T) {
	tests := []struct {
		name    string
		rule    string
		path    string
		symbol  string
		context string
	}{
		{name: "traversal", rule: "UPD101", path: "../outside.cs", context: "target=data"},
		{name: "absolute drive", rule: "UPD101", path: "C:/outside.cs", context: "target=data"},
		{name: "empty path", rule: "UPD101", path: ".", context: "target=data"},
		{name: "empty context", rule: "UPD101", path: "src/file.cs"},
		{name: "bad rule", rule: "UPD1", path: "src/file.cs", context: "target=data"},
		{name: "nul context", rule: "UPD101", path: "src/file.cs", context: "a\x00b"},
	}
	for _, test := range tests {
		t.Run(test.name, func(t *testing.T) {
			if _, err := FindingFingerprint(test.rule, test.path, test.symbol, test.context); err == nil {
				t.Fatal("expected invalid identity error")
			}
		})
	}
}
