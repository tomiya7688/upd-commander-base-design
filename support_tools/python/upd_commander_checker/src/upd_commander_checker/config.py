import json
import sys
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class CheckerConfig:
    input_path: str = "."
    output_path: str = ""
    ignore: tuple[str, ...] = ()
    warnings_as_errors: bool = False


def load_config() -> CheckerConfig:
    path = _find_config_path()
    if path is None:
        return CheckerConfig()

    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError):
        return CheckerConfig()

    base = path.parent.parent
    input_path = _resolve_path(base, str(data.get("input", ".")))
    output_value = str(data.get("output", ""))
    output_path = _resolve_path(base, output_value) if output_value else ""
    ignore_value = data.get("ignore", [])
    ignore = tuple(str(item) for item in ignore_value) if isinstance(ignore_value, list) else ()
    warnings_as_errors = bool(data.get("warnings_as_errors", False))
    return CheckerConfig(input_path, output_path, ignore, warnings_as_errors)


def _find_config_path() -> Path | None:
    candidates = [Path.cwd() / "config" / "path.json"]
    executable = Path(sys.executable).resolve().parent / "config" / "path.json"
    if executable not in candidates:
        candidates.insert(0, executable)
    for path in candidates:
        if path.is_file():
            return path
    return None


def _resolve_path(base: Path, value: str) -> str:
    path = Path(value)
    if path.is_absolute():
        return str(path)
    return str((base / path).resolve())
