from pathlib import Path

from .models import ModuleInfo


_LAYER_NAMES = {"ui", "process", "data"}
_ROLE_NAMES = {"commander", "messenger", "processing"}
_APPLICATION_MARKERS = {"app", "apps", "application", "applications", "feature", "features"}


def classify_module(path: Path) -> ModuleInfo:
    directory_parts = [part.lower() for part in path.parent.parts]
    stem = path.stem.lower().replace("-", "_")

    layer = _find_name(directory_parts, _LAYER_NAMES)
    role = _find_path_role(directory_parts, stem)
    application = _find_application(directory_parts)
    return ModuleInfo(path=path, layer=layer, role=role, application=application)


def classify_import(module_name: str) -> tuple[str | None, str | None, str | None]:
    path_parts = module_name.lower().replace("-", "_").split(".")
    token_parts: list[str] = []
    for part in path_parts:
        token_parts.extend(part.split("_"))

    layer = _find_name(token_parts, _LAYER_NAMES)
    role = _find_role(token_parts)
    application = _find_application(path_parts)
    return layer, role, application


def _find_name(parts: list[str], candidates: set[str]) -> str | None:
    for part in parts:
        if part in candidates:
            return part
    return None


def _find_path_role(directory_parts: list[str], stem: str) -> str | None:
    directory_role = _find_name(directory_parts, _ROLE_NAMES)
    if directory_role:
        return directory_role
    for role in _ROLE_NAMES:
        if stem == role or stem.endswith("_" + role):
            return role
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


def _find_application(parts: list[str]) -> str | None:
    for index, part in enumerate(parts[:-1]):
        if part in _APPLICATION_MARKERS:
            return parts[index + 1]
    return None
