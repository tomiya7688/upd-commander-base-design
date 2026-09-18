import json
import sys
from pathlib import Path

from .config_error import ConfigError
from .config_model import CheckerConfig
from .rule_selection import normalize_enabled_rules


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
    if "upd301_max_inputs" in data and (
        type(data["upd301_max_inputs"]) is not int or data["upd301_max_inputs"] < 1
    ):
        raise ConfigError("invalid config field: upd301_max_inputs")
    if "enabled_rules" in data and (
        not isinstance(data["enabled_rules"], list)
        or not all(isinstance(item, str) for item in data["enabled_rules"])
    ):
        raise ConfigError("invalid config field: enabled_rules")

    base = path.parent.parent
    input_path = _resolve_path(base, data.get("input", "."))
    output_value = data.get("output", "")
    output_path = _resolve_path(base, output_value) if output_value else ""
    ignore = tuple(data.get("ignore", []))
    warnings_as_errors = data.get("warnings_as_errors", False)
    upd301_max_inputs = data.get("upd301_max_inputs", 2)
    enabled_rules = None
    if "enabled_rules" in data:
        try:
            enabled_rules = normalize_enabled_rules(data["enabled_rules"])
        except ValueError as exc:
            raise ConfigError("invalid config field: enabled_rules") from exc
    return CheckerConfig(
        input_path=input_path,
        output_path=output_path,
        ignore=ignore,
        warnings_as_errors=warnings_as_errors,
        enabled_rules=enabled_rules,
        upd301_max_inputs=upd301_max_inputs,
    )


def config_path_for_write() -> Path:
    if getattr(sys, "frozen", False):
        return Path(sys.executable).resolve().parent / "config" / "path.json"
    return Path.cwd() / "config" / "path.json"


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
