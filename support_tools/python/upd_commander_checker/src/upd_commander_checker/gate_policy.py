import re
from dataclasses import replace

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
