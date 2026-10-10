from dataclasses import dataclass


@dataclass(frozen=True)
class GateException:
    rule: str
    path: str
    reason: str
    line: int | None = None
