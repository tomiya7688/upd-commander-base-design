# Finding Baseline Contract

[日本語](baseline-contract.md) | **English**

This document defines version 1 of Finding identity and the baseline file. All four language checkers must produce the same fingerprint for the same canonical inputs.

## 1. Finding identity

The fingerprint uses these four fields:

| Field | Required | Canonical form |
|---|---:|---|
| `rule` | Yes | `UPD` followed by at least three digits, uppercase |
| `path` | Yes | Repository-root-relative, `/` separators, remove `.` components, reject `..` and absolute paths |
| `symbol` | Yes | Qualified type/function/member; empty string if unavailable |
| `context` | Yes | Stable rule-specific identity context; must not be empty |

Normalize strings to Unicode NFC and preserve path case. Do not use display prose or absolute paths as symbol/context. When a rule can produce multiple Findings with the same rule in one file, stable information must distinguish them. For example, dependency context should include the canonical target identity.

Exclude line/column numbers, nearby source lines, severity, display message, timestamps, absolute paths, and environment-specific values. Therefore, inserting lines or changing severity/display prose alone does not change identity; changing rule/path/symbol/context creates a different identity. Context is semantic and stable, not a substitute for a line number.

## 2. Fingerprint algorithm v1

1. Apply the canonicalization above.
2. Join these five UTF-8 strings in order using a single U+0000 byte; U+0000 is forbidden inside fields:

   `upd-finding-fingerprint-v1`, `rule`, `path`, `symbol`, `context`

3. Compute SHA-256 over the resulting bytes and encode it as lowercase hex prefixed by `sha256:`.

The fixed domain separator and field order are part of version 1. Increase the fingerprint version for incompatible changes to canonicalization or fields.

If a scan produces the same fingerprint more than once, the checker must not silently collapse the Findings. Report insufficient context so their identities can be made unique.

## 3. Baseline JSON schema v1

```json
{
  "schema_version": 1,
  "fingerprint_version": 1,
  "findings": [
    {
      "fingerprint": "sha256:<64 lowercase hex characters>",
      "rule": "UPD101",
      "path": "src/ui/screen.cs",
      "symbol": "Ui.Screen.Run",
      "context": "target=data.storage",
      "severity": "error",
      "line": 24,
      "message": "UI must not depend on Data"
    }
  ]
}
```

`schema_version`, `fingerprint_version`, `findings`, and each entry's `fingerprint`, `rule`, `path`, `symbol`, `context`, and `severity` are required. `line` and `message` are optional display/tracking metadata and do not affect identity. An entry's fingerprint must match the value recomputed from its canonical identity. Fingerprints must be unique within a baseline.

## 4. Compatibility and errors

- Ignore unknown additive properties within a supported schema version.
- Do not guess when a schema or fingerprint version is unsupported; report both the supported and file versions.
- Read an older version only when an explicit migration exists; otherwise report an unsupported-version error.
- Missing required fields, type mismatches, invalid fingerprints/paths, or duplicate fingerprints make the baseline invalid.
- An empty `findings` array is a valid baseline.

## 5. Shared fixtures

[baseline-fingerprint-fixtures.json](baseline-fingerprint-fixtures.json) defines golden vectors. Fixture tests verify stability across line/severity/canonical spelling changes and separation when rule/path/symbol/context changes.
