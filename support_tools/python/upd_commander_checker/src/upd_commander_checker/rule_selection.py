from collections.abc import Iterable

from .finding import Finding


SUPPORTED_RULES = (
    "UPD001",
    "UPD002",
    "UPD101",
    "UPD102",
    "UPD103",
    "UPD201",
    "UPD202",
    "UPD203",
    "UPD301",
    "UPD302",
    "UPD303",
    "UPD401",
    "UPD402",
    "UPD403",
    "UPD404",
    "UPD405",
)
_SUPPORTED_RULE_SET = frozenset(SUPPORTED_RULES)


def normalize_enabled_rules(values: Iterable[str]) -> tuple[str, ...]:
    normalized = tuple(dict.fromkeys(value.strip().upper() for value in values))
    if any(value not in _SUPPORTED_RULE_SET for value in normalized):
        raise ValueError("enabled_rules contains an unsupported UPD code")
    return normalized


def filter_enabled_findings(
    findings: Iterable[Finding], enabled_rules: tuple[str, ...] | None
) -> list[Finding]:
    if enabled_rules is None:
        return list(findings)
    enabled = frozenset(enabled_rules)
    return [finding for finding in findings if finding.code in enabled]
