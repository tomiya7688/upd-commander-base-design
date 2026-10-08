import re
from dataclasses import replace

from .config_model import GateException
from .finding import Finding

SEVERITIES = frozenset({"error", "warning", "attention"})
_RULE_PATTERN = re.compile(r"^UPD[0-9]{3,}$")


def normalize_fail_on(value: object) -> tuple[str, ...]:
    if not isinstance(value, list) or not all(isinstance(item, str) for item in value):
        raise ValueError("must be a list of severity names")
    result: list[str] = []
    for item in value:
        severity = item.strip().lower()
        if severity not in SEVERITIES:
            raise ValueError("contains an unsupported severity")
        if severity not in result:
            result.append(severity)
    return tuple(result)


def normalize_severity_overrides(value: object) -> tuple[tuple[str, str], ...]:
    if not isinstance(value, dict):
        raise ValueError("must be an object mapping UPD rules to severities")
    result: dict[str, str] = {}
    for rule, severity in value.items():
        if not isinstance(rule, str) or not _RULE_PATTERN.fullmatch(rule.upper()):
            raise ValueError("contains an invalid UPD rule")
        if not isinstance(severity, str) or severity.lower() not in SEVERITIES:
            raise ValueError("contains an unsupported severity")
        result[rule.upper()] = severity.lower()
    return tuple(sorted(result.items()))


def normalize_gate_exceptions(value: object) -> tuple[GateException, ...]:
    if not isinstance(value, list):
        raise ValueError("must be a list of gate exception objects")
    result: list[GateException] = []
    seen: set[tuple[str, str, int | None]] = set()
    for item in value:
        if not isinstance(item, dict) or set(item) - {"rule", "path", "reason", "line"}:
            raise ValueError("contains an invalid gate exception")
        rule = item.get("rule")
        path = item.get("path")
        reason = item.get("reason")
        line = item.get("line")
        if not isinstance(rule, str) or not _RULE_PATTERN.fullmatch(rule.upper()):
            raise ValueError("contains an invalid UPD rule")
        if not isinstance(path, str) or not _is_exact_relative_path(path):
            raise ValueError("path must be an exact relative path using forward slashes")
        if not isinstance(reason, str) or not reason.strip() or "\n" in reason or "\r" in reason:
            raise ValueError("reason must be a non-empty single-line value")
        if line is not None and (type(line) is not int or line < 1):
            raise ValueError("line must be a positive integer")
        key = (rule.upper(), path, line)
        if key in seen:
            raise ValueError("contains a duplicate gate exception")
        seen.add(key)
        result.append(GateException(key[0], path, reason.strip(), line))
    return tuple(result)


def _is_exact_relative_path(value: str) -> bool:
    return bool(value) and not any(char in value for char in "\\:*?[]") and not value.startswith("/") and all(
        segment not in {"", ".", ".."} for segment in value.split("/")
    )


def parse_fail_on_argument(value: str) -> tuple[str, ...]:
    if value == "":
        return ()
    return normalize_fail_on(value.split(","))


def parse_severity_override_argument(value: str) -> tuple[str, str]:
    rule, separator, severity = value.partition("=")
    if not separator:
        raise ValueError("expected UPDnnn=severity")
    normalized = normalize_severity_overrides({rule: severity})
    return normalized[0]


def apply_severity_overrides(
    findings: list[Finding], overrides: dict[str, str]
) -> list[Finding]:
    return [
        replace(finding, severity=overrides.get(finding.code.upper(), finding.severity))
        for finding in findings
    ]


def should_fail(findings: list[Finding], fail_on: tuple[str, ...]) -> bool:
    return any(finding.severity in fail_on for finding in findings)


def exception_reason(
    finding: Finding, display_path: str, exceptions: tuple[GateException, ...]
) -> str | None:
    for exception in exceptions:
        if (
            exception.rule == finding.code.upper()
            and exception.path == display_path
            and exception.line == finding.line
        ):
            return exception.reason
    for exception in exceptions:
        if (
            exception.rule == finding.code.upper()
            and exception.path == display_path
            and exception.line is None
        ):
            return exception.reason
    return None
