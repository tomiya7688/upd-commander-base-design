from dataclasses import dataclass


@dataclass(frozen=True)
class DependencyRuleResult:
    code: str
    message: str
    severity: str
