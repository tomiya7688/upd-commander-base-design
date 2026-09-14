import json
import sys
from dataclasses import dataclass
from pathlib import Path


class ConfigError(Exception):
    pass


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
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise ConfigError(f"invalid config: {path}") from exc

    if not isinstance(data, dict):
        raise ConfigError(f"invalid config: {path}")
    if "input" in data and not isinstance(data["input"], str):
        raise ConfigError("invalid config field: input")
    if "output" in data and not isinstance(data["output"], str):
        raise ConfigError("invalid config field: output")
    if "ignore" in data and (
        not isinstance(data["ignore"], list)
        or not all(isinstance(item, str) for item in data["ignore"])
    ):
        raise ConfigError("invalid config field: ignore")
    if "warnings_as_errors" in data and not isinstance(data["warnings_as_errors"], bool):
        raise ConfigError("invalid config field: warnings_as_errors")

    base = path.parent.parent
    input_path = _resolve_path(base, data.get("input", "."))
    output_value = data.get("output", "")
    output_path = _resolve_path(base, output_value) if output_value else ""
    ignore = tuple(data.get("ignore", []))
    warnings_as_errors = data.get("warnings_as_errors", False)
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
