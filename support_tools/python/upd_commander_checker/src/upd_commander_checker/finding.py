from dataclasses import dataclass, field
from pathlib import Path


@dataclass(frozen=True)
class Finding:
    path: Path
    line: int
    code: str
    message: str
    severity: str = "error"
    symbol: str = field(default="", compare=False)
    context: str = field(default="", compare=False)
