"""Versioned Finding identity and baseline serialization helpers."""

from __future__ import annotations

from collections.abc import Iterable, Mapping
import hashlib
import json
import re
import unicodedata
from pathlib import Path, PurePosixPath
from typing import Any


SCHEMA_VERSION = 1
FINGERPRINT_VERSION = 1
_DOMAIN_SEPARATOR = "upd-finding-fingerprint-v1"
_RULE_PATTERN = re.compile(r"UPD[0-9]{3,}")
_FINGERPRINT_PATTERN = re.compile(r"sha256:[0-9a-f]{64}")
_REQUIRED_ENTRY_FIELDS = {
    "fingerprint",
    "rule",
    "path",
    "symbol",
    "context",
    "severity",
}


class BaselineError(ValueError):
    """A malformed baseline or ambiguous finding identity."""


def finding_fingerprint(
    rule: str, path: str, symbol: str, context: str
) -> str:
    """Return the v1 fingerprint for canonical identity fields."""
    canonical_rule = unicodedata.normalize("NFC", rule).upper()
    if _RULE_PATTERN.fullmatch(canonical_rule) is None:
        raise BaselineError("rule must match UPD followed by at least three digits")

    canonical_path = unicodedata.normalize("NFC", path.replace("\\", "/"))
    parsed_path = PurePosixPath(canonical_path)
    if (
        parsed_path.is_absolute()
        or re.match(r"^[A-Za-z]:", canonical_path)
        or canonical_path.startswith("//")
        or ".." in parsed_path.parts
    ):
        raise BaselineError("path must be repository-relative")
    canonical_path = "/".join(
        part for part in parsed_path.parts if part not in {"", "."}
    )
    if not canonical_path:
        raise BaselineError("path must not be empty")

    canonical_symbol = unicodedata.normalize("NFC", symbol)
    canonical_context = unicodedata.normalize("NFC", context)
    fields = (
        _DOMAIN_SEPARATOR,
        canonical_rule,
        canonical_path,
        canonical_symbol,
        canonical_context,
    )
    if not canonical_context:
        raise BaselineError("context must not be empty")
    if any("\0" in field for field in fields):
        raise BaselineError("identity fields must not contain NUL")
    payload = "\0".join(fields).encode("utf-8")
    return "sha256:" + hashlib.sha256(payload).hexdigest()


def build_baseline(findings: Iterable[Mapping[str, Any]]) -> dict[str, Any]:
    """Build a deterministic v1 baseline from canonical Finding mappings."""
    entries: list[dict[str, Any]] = []
    seen: set[str] = set()
    for finding in findings:
        entry = _entry_from_finding(finding)
        fingerprint = entry["fingerprint"]
        if fingerprint in seen:
            raise BaselineError(
                f"duplicate Finding identity in scan: {fingerprint}"
            )
        seen.add(fingerprint)
        entries.append(entry)
    entries.sort(key=lambda item: item["fingerprint"])
    return {
        "schema_version": SCHEMA_VERSION,
        "fingerprint_version": FINGERPRINT_VERSION,
        "findings": entries,
    }


def write_baseline(path: Path, findings: Iterable[Mapping[str, Any]]) -> None:
    """Write a validated, deterministic baseline JSON document."""
    baseline = build_baseline(findings)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps(baseline, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


def load_baseline(path: Path) -> dict[str, Any]:
    """Read and strictly validate a v1 baseline document."""
    try:
        document = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise BaselineError(f"cannot read baseline: {exc}") from exc
    return validate_baseline(document)


def validate_baseline(document: Any) -> dict[str, Any]:
    if not isinstance(document, dict):
        raise BaselineError("baseline root must be an object")
    schema_version = document.get("schema_version")
    fingerprint_version = document.get("fingerprint_version")
    if type(schema_version) is not int or schema_version != SCHEMA_VERSION:
        raise BaselineError(
            f"unsupported schema_version {schema_version!r}; supported version is {SCHEMA_VERSION}"
        )
    if (
        type(fingerprint_version) is not int
        or fingerprint_version != FINGERPRINT_VERSION
    ):
        raise BaselineError(
            "unsupported fingerprint_version "
            f"{fingerprint_version!r}; supported version is {FINGERPRINT_VERSION}"
        )
    findings = document.get("findings")
    if not isinstance(findings, list):
        raise BaselineError("findings must be an array")

    validated: list[dict[str, Any]] = []
    seen: set[str] = set()
    for index, finding in enumerate(findings):
        if not isinstance(finding, dict):
            raise BaselineError(f"findings[{index}] must be an object")
        missing = _REQUIRED_ENTRY_FIELDS - finding.keys()
        if missing:
            raise BaselineError(
                f"findings[{index}] is missing required fields: {', '.join(sorted(missing))}"
            )
        rule = _string_field(finding, "rule", index)
        path = _string_field(finding, "path", index)
        symbol = _string_field(finding, "symbol", index)
        context = _string_field(finding, "context", index)
        severity = _string_field(finding, "severity", index)
        fingerprint = _string_field(finding, "fingerprint", index)
        if severity not in {"error", "warning", "attention"}:
            raise BaselineError(f"findings[{index}].severity is invalid")
        if _FINGERPRINT_PATTERN.fullmatch(fingerprint) is None:
            raise BaselineError(f"findings[{index}].fingerprint is invalid")
        expected = finding_fingerprint(rule, path, symbol, context)
        if fingerprint != expected:
            raise BaselineError(f"findings[{index}].fingerprint does not match identity")
        if fingerprint in seen:
            raise BaselineError(f"duplicate fingerprint: {fingerprint}")
        seen.add(fingerprint)

        entry = {
            "fingerprint": fingerprint,
            "rule": rule,
            "path": path,
            "symbol": symbol,
            "context": context,
            "severity": severity,
        }
        if "line" in finding:
            line = finding["line"]
            if type(line) is not int or line < 1:
                raise BaselineError(f"findings[{index}].line must be a positive integer")
            entry["line"] = line
        if "message" in finding:
            entry["message"] = _string_field(finding, "message", index)
        validated.append(entry)

    return {
        "schema_version": schema_version,
        "fingerprint_version": fingerprint_version,
        "findings": validated,
    }


def compare_findings(
    current: Iterable[Mapping[str, Any]], baseline: Mapping[str, Any]
) -> dict[str, list[dict[str, Any]]]:
    """Classify current and baseline findings without discarding either set."""
    validated = validate_baseline(dict(baseline))
    current_by_fingerprint: dict[str, dict[str, Any]] = {}
    for finding in current:
        entry = _entry_from_finding(finding)
        fingerprint = entry["fingerprint"]
        if fingerprint in current_by_fingerprint:
            raise BaselineError(f"duplicate Finding identity in scan: {fingerprint}")
        current_by_fingerprint[fingerprint] = entry

    baseline_by_fingerprint = {
        entry["fingerprint"]: entry for entry in validated["findings"]
    }
    new = [
        _with_status(entry, "NEW")
        for fingerprint, entry in current_by_fingerprint.items()
        if fingerprint not in baseline_by_fingerprint
    ]
    existing = [
        _with_status(current_by_fingerprint[fingerprint], "EXISTING")
        for fingerprint in current_by_fingerprint.keys() & baseline_by_fingerprint.keys()
    ]
    resolved = [
        _with_status(entry, "RESOLVED")
        for fingerprint, entry in baseline_by_fingerprint.items()
        if fingerprint not in current_by_fingerprint
    ]
    for group in (new, existing, resolved):
        group.sort(key=lambda entry: entry["fingerprint"])
    return {"new": new, "existing": existing, "resolved": resolved}


def _entry_from_finding(finding: Mapping[str, Any]) -> dict[str, Any]:
    try:
        rule = finding["rule"]
        path = finding["path"]
        symbol = finding["symbol"]
        context = finding["context"]
        severity = finding["severity"]
    except KeyError as exc:
        raise BaselineError(f"Finding is missing required identity field: {exc.args[0]}") from exc
    for name, value in (
        ("rule", rule),
        ("path", path),
        ("symbol", symbol),
        ("context", context),
        ("severity", severity),
    ):
        if not isinstance(value, str):
            raise BaselineError(f"Finding {name} must be a string")
    fingerprint = finding_fingerprint(rule, path, symbol, context)
    entry: dict[str, Any] = {
        "fingerprint": fingerprint,
        "rule": unicodedata.normalize("NFC", rule).upper(),
        "path": _canonical_path(path),
        "symbol": unicodedata.normalize("NFC", symbol),
        "context": unicodedata.normalize("NFC", context),
        "severity": severity,
    }
    if severity not in {"error", "warning", "attention"}:
        raise BaselineError("Finding severity is invalid")
    if "line" in finding:
        line = finding["line"]
        if type(line) is not int or line < 1:
            raise BaselineError("Finding line must be a positive integer")
        entry["line"] = line
    if "message" in finding:
        message = finding["message"]
        if not isinstance(message, str):
            raise BaselineError("Finding message must be a string")
        entry["message"] = message
    return entry


def _canonical_path(path: str) -> str:
    normalized = unicodedata.normalize("NFC", path.replace("\\", "/"))
    parsed = PurePosixPath(normalized)
    return "/".join(part for part in parsed.parts if part not in {"", "."})


def _string_field(finding: Mapping[str, Any], name: str, index: int) -> str:
    value = finding[name]
    if not isinstance(value, str):
        raise BaselineError(f"findings[{index}].{name} must be a string")
    return value


def _with_status(entry: Mapping[str, Any], status: str) -> dict[str, Any]:
    result = dict(entry)
    result["status"] = status
    return result
