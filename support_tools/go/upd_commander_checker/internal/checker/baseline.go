package checker

import (
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"io"
	"os"
	"path"
	"path/filepath"
	"regexp"
	"sort"
	"strings"

	"golang.org/x/text/unicode/norm"
)

const BaselineSchemaVersion = 1
const BaselineFingerprintVersion = 1

// BaselineEntry stores identity fields and optional display metadata.
type BaselineEntry struct {
	Fingerprint string `json:"fingerprint"`
	Rule        string `json:"rule"`
	Path        string `json:"path"`
	Symbol      string `json:"symbol"`
	Context     string `json:"context"`
	Severity    string `json:"severity"`
	Line        int    `json:"line,omitempty"`
	Message     string `json:"message,omitempty"`
}

// Baseline is the portable versioned finding snapshot.
type Baseline struct {
	SchemaVersion      int             `json:"schema_version"`
	FingerprintVersion int             `json:"fingerprint_version"`
	Findings           []BaselineEntry `json:"findings"`
}

func (entry *BaselineEntry) UnmarshalJSON(data []byte) error {
	type alias BaselineEntry
	var fields map[string]json.RawMessage
	if err := json.Unmarshal(data, &fields); err != nil {
		return err
	}
	for _, name := range []string{"fingerprint", "rule", "path", "symbol", "context", "severity"} {
		raw, ok := fields[name]
		if !ok {
			return fmt.Errorf("missing required field %q", name)
		}
		var value string
		if err := json.Unmarshal(raw, &value); err != nil || len(raw) == 0 || raw[0] != '"' {
			return fmt.Errorf("field %q must be a string", name)
		}
	}
	if raw, ok := fields["message"]; ok {
		var message string
		if err := json.Unmarshal(raw, &message); err != nil || len(raw) == 0 || raw[0] != '"' {
			return fmt.Errorf("message must be a string")
		}
	}
	if raw, ok := fields["line"]; ok {
		var line int
		if err := json.Unmarshal(raw, &line); err != nil || line < 1 {
			return fmt.Errorf("line must be a positive integer")
		}
	}
	var decoded alias
	if err := json.Unmarshal(data, &decoded); err != nil {
		return err
	}
	*entry = BaselineEntry(decoded)
	return nil
}

func (baseline *Baseline) UnmarshalJSON(data []byte) error {
	type alias Baseline
	var fields map[string]json.RawMessage
	if err := json.Unmarshal(data, &fields); err != nil {
		return err
	}
	for _, name := range []string{"schema_version", "fingerprint_version", "findings"} {
		if _, ok := fields[name]; !ok {
			return fmt.Errorf("missing required field %q", name)
		}
	}
	if string(fields["findings"]) == "null" {
		return fmt.Errorf("findings must be an array")
	}
	var decoded alias
	if err := json.Unmarshal(data, &decoded); err != nil {
		return err
	}
	*baseline = Baseline(decoded)
	return nil
}

const findingFingerprintDomainV1 = "upd-finding-fingerprint-v1"

var baselineRulePattern = regexp.MustCompile(`^UPD[0-9]{3,}$`)
var drivePathPattern = regexp.MustCompile(`^[A-Za-z]:/`)

// CanonicalFindingPath normalizes a repository-relative path for baseline identity.
func CanonicalFindingPath(value string) (string, error) {
	canonical := norm.NFC.String(strings.ReplaceAll(value, "\\", "/"))
	if path.IsAbs(canonical) || strings.HasPrefix(canonical, "//") || drivePathPattern.MatchString(canonical) {
		return "", fmt.Errorf("path must be repository-relative")
	}
	for _, part := range strings.Split(canonical, "/") {
		if part == ".." {
			return "", fmt.Errorf("path must not contain '..'")
		}
	}
	canonical = path.Clean(canonical)
	if canonical == "." || canonical == "" {
		return "", fmt.Errorf("path must not be empty")
	}
	return canonical, nil
}

// FindingFingerprint returns the version 1 SHA-256 identity shared by all checkers.
func FindingFingerprint(rule string, findingPath string, symbol string, context string) (string, error) {
	canonicalRule := strings.ToUpper(norm.NFC.String(rule))
	if !baselineRulePattern.MatchString(canonicalRule) {
		return "", fmt.Errorf("rule must match UPD followed by at least three digits")
	}
	canonicalPath, err := CanonicalFindingPath(findingPath)
	if err != nil {
		return "", err
	}
	canonicalSymbol := norm.NFC.String(symbol)
	canonicalContext := norm.NFC.String(context)
	if canonicalContext == "" {
		return "", fmt.Errorf("context must not be empty")
	}
	fields := []string{findingFingerprintDomainV1, canonicalRule, canonicalPath, canonicalSymbol, canonicalContext}
	for _, field := range fields {
		if strings.ContainsRune(field, '\x00') {
			return "", fmt.Errorf("identity fields must not contain NUL")
		}
	}
	digest := sha256.Sum256([]byte(strings.Join(fields, "\x00")))
	return "sha256:" + hex.EncodeToString(digest[:]), nil
}

// BuildBaseline validates identities and returns a deterministic baseline.
func BuildBaseline(findings []BaselineEntry) (Baseline, error) {
	entries := make([]BaselineEntry, 0, len(findings))
	seen := make(map[string]struct{}, len(findings))
	for _, finding := range findings {
		entry, err := canonicalBaselineEntry(finding, false)
		if err != nil {
			return Baseline{}, err
		}
		if _, exists := seen[entry.Fingerprint]; exists {
			return Baseline{}, fmt.Errorf("duplicate Finding identity in scan: %s", entry.Fingerprint)
		}
		seen[entry.Fingerprint] = struct{}{}
		entries = append(entries, entry)
	}
	sort.Slice(entries, func(i, j int) bool { return entries[i].Fingerprint < entries[j].Fingerprint })
	return Baseline{SchemaVersion: BaselineSchemaVersion, FingerprintVersion: BaselineFingerprintVersion, Findings: entries}, nil
}

// ValidateBaseline validates schema versions, entries, and recomputed fingerprints.
func ValidateBaseline(baseline Baseline) (Baseline, error) {
	if baseline.SchemaVersion != BaselineSchemaVersion {
		return Baseline{}, fmt.Errorf("unsupported schema_version %d; supported version is %d", baseline.SchemaVersion, BaselineSchemaVersion)
	}
	if baseline.FingerprintVersion != BaselineFingerprintVersion {
		return Baseline{}, fmt.Errorf("unsupported fingerprint_version %d; supported version is %d", baseline.FingerprintVersion, BaselineFingerprintVersion)
	}
	seen := make(map[string]struct{}, len(baseline.Findings))
	for i, finding := range baseline.Findings {
		entry, err := canonicalBaselineEntry(finding, true)
		if err != nil {
			return Baseline{}, fmt.Errorf("findings[%d]: %w", i, err)
		}
		if _, exists := seen[entry.Fingerprint]; exists {
			return Baseline{}, fmt.Errorf("duplicate fingerprint: %s", entry.Fingerprint)
		}
		seen[entry.Fingerprint] = struct{}{}
		baseline.Findings[i] = entry
	}
	return baseline, nil
}

// WriteBaseline writes a deterministic, indented UTF-8 JSON snapshot.
func WriteBaseline(filename string, findings []BaselineEntry) error {
	baseline, err := BuildBaseline(findings)
	if err != nil {
		return err
	}
	if dir := filepath.Dir(filename); dir != "." {
		if err := os.MkdirAll(dir, 0o755); err != nil {
			return fmt.Errorf("cannot write baseline: %w", err)
		}
	}
	data, err := json.MarshalIndent(baseline, "", "  ")
	if err != nil {
		return err
	}
	return os.WriteFile(filename, append(data, '\n'), 0o644)
}

// LoadBaseline reads and validates a baseline document.
func LoadBaseline(filename string) (Baseline, error) {
	data, err := os.ReadFile(filename)
	if err != nil {
		return Baseline{}, fmt.Errorf("cannot read baseline: %w", err)
	}
	decoder := json.NewDecoder(strings.NewReader(string(data)))
	var baseline Baseline
	if err := decoder.Decode(&baseline); err != nil {
		return Baseline{}, fmt.Errorf("cannot read baseline: %w", err)
	}
	var extra any
	if err := decoder.Decode(&extra); err != io.EOF {
		return Baseline{}, fmt.Errorf("cannot read baseline: trailing JSON data")
	}
	return ValidateBaseline(baseline)
}

// CompareBaseline classifies current findings against a validated snapshot.
func CompareBaseline(current []BaselineEntry, baseline Baseline) (ClassifiedBaseline, error) {
	validated, err := ValidateBaseline(baseline)
	if err != nil {
		return ClassifiedBaseline{}, err
	}
	currentByID := make(map[string]BaselineEntry, len(current))
	for _, finding := range current {
		entry, err := canonicalBaselineEntry(finding, false)
		if err != nil {
			return ClassifiedBaseline{}, err
		}
		if _, exists := currentByID[entry.Fingerprint]; exists {
			return ClassifiedBaseline{}, fmt.Errorf("duplicate Finding identity in scan: %s", entry.Fingerprint)
		}
		currentByID[entry.Fingerprint] = entry
	}
	baselineByID := make(map[string]BaselineEntry, len(validated.Findings))
	for _, entry := range validated.Findings {
		baselineByID[entry.Fingerprint] = entry
	}
	result := ClassifiedBaseline{New: []BaselineEntry{}, Existing: []BaselineEntry{}, Resolved: []BaselineEntry{}}
	for id, entry := range currentByID {
		if _, exists := baselineByID[id]; exists {
			result.Existing = append(result.Existing, entry)
		} else {
			result.New = append(result.New, entry)
		}
	}
	for id, entry := range baselineByID {
		if _, exists := currentByID[id]; !exists {
			result.Resolved = append(result.Resolved, entry)
		}
	}
	for _, group := range []*[]BaselineEntry{&result.New, &result.Existing, &result.Resolved} {
		sort.Slice(*group, func(i, j int) bool { return (*group)[i].Fingerprint < (*group)[j].Fingerprint })
	}
	return result, nil
}

func canonicalBaselineEntry(entry BaselineEntry, verifyFingerprint bool) (BaselineEntry, error) {
	fingerprint, err := FindingFingerprint(entry.Rule, entry.Path, entry.Symbol, entry.Context)
	if err != nil {
		return BaselineEntry{}, err
	}
	entry.Rule = strings.ToUpper(norm.NFC.String(entry.Rule))
	entry.Path, err = CanonicalFindingPath(entry.Path)
	if err != nil {
		return BaselineEntry{}, err
	}
	entry.Symbol = norm.NFC.String(entry.Symbol)
	entry.Context = norm.NFC.String(entry.Context)
	if entry.Severity != "error" && entry.Severity != "warning" && entry.Severity != "attention" {
		return BaselineEntry{}, fmt.Errorf("severity is invalid")
	}
	if entry.Line < 0 {
		return BaselineEntry{}, fmt.Errorf("line must be a positive integer")
	}
	if verifyFingerprint && entry.Fingerprint != fingerprint {
		return BaselineEntry{}, fmt.Errorf("fingerprint does not match identity")
	}
	entry.Fingerprint = fingerprint
	return entry, nil
}
