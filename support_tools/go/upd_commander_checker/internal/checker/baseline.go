package checker

import (
	"crypto/sha256"
	"encoding/hex"
	"fmt"
	"path"
	"regexp"
	"strings"

	"golang.org/x/text/unicode/norm"
)

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
