from pathlib import Path

from .models import ModuleInfo


_LAYER_NAMES = {"ui", "process", "data"}
_ROLE_NAMES = {"commander", "messenger", "processing"}


def classify_module(path: Path) -> ModuleInfo:
    parts = [part.lower() for part in path.parts]
    stem_parts = path.stem.lower().replace("-", "_").split("_")

    layer = _find_name(parts + stem_parts, _LAYER_NAMES)
    role = _find_role(parts + stem_parts)
    return ModuleInfo(path=path, layer=layer, role=role)


def classify_import(module_name: str) -> tuple[str | None, str | None]:
    parts = module_name.lower().replace("-", "_").replace(".", "_").split("_")
    layer = _find_name(parts, _LAYER_NAMES)
    role = _find_role(parts)
    return layer, role


def _find_name(parts: list[str], candidates: set[str]) -> str | None:
    for part in parts:
        if part in candidates:
            return part
    return None


def _find_role(parts: list[str]) -> str | None:
    for part in parts:
        if part in _ROLE_NAMES:
            return part
        if part.endswith("commander"):
            return "commander"
        if part.endswith("messenger"):
            return "messenger"
        if part.endswith("processing"):
            return "processing"
    return None
