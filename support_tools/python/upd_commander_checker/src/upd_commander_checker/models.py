from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class Finding:
    path: Path
    line: int
    code: str
    message: str
    severity: str = "error"


@dataclass(frozen=True)
class ModuleInfo:
    path: Path
    layer: str | None
    role: str | None
