from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class ModuleInfo:
    path: Path
    layer: str | None
    role: str | None
    application: str | None = None
