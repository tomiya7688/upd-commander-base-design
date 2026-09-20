from collections.abc import Iterable
from pathlib import Path

from .models import ModuleInfo


_LAYER_NAMES = {"ui", "process", "data"}
_ROLE_NAMES = {"commander", "messenger", "processing", "compresser"}
_APPLICATION_MARKERS = {"app", "apps", "application", "applications", "feature", "features"}
_DEFAULT_COMMON_ROOTS = ("common", "shared")


def classify_module(
    path: Path,
    root: Path | None = None,
    common_roots: tuple[str, ...] = _DEFAULT_COMMON_ROOTS,
) -> ModuleInfo:
    module_path = _relative_module_path(path, root)
    directory_parts = [part.lower() for part in module_path.parent.parts]
    stem = module_path.stem.lower().replace("-", "_")
    scoped_parts = _application_scope(directory_parts)

    layer = _find_layer(scoped_parts, common_roots)
    role = _find_path_role(scoped_parts, stem)
    application = _find_application(directory_parts)
    return ModuleInfo(path=path, layer=layer, role=role, application=application)


def classify_import(
    module_name: str,
    common_roots: tuple[str, ...] = _DEFAULT_COMMON_ROOTS,
) -> ModuleInfo:
    path_parts = module_name.lower().replace("-", "_").split(".")
    scoped_parts = _application_scope(path_parts)
    token_parts: list[str] = []
    for part in scoped_parts:
        token_parts.extend(part.split("_"))

    layer = _find_layer(token_parts, common_roots)
    role = _find_role(list(reversed(token_parts)))
    application = _find_application(path_parts)
    return ModuleInfo(
        path=Path(module_name),
        layer=layer,
        role=role,
        application=application,
    )


def _relative_module_path(path: Path, root: Path | None) -> Path:
    if root is None:
        return path
    try:
        return path.relative_to(root)
    except ValueError:
        return path


def _find_name(parts: Iterable[str], candidates: set[str]) -> str | None:
    for part in parts:
        if part in candidates:
            return part
    return None


def _find_path_role(directory_parts: list[str], stem: str) -> str | None:
    directory_role = _find_name(reversed(directory_parts), _ROLE_NAMES)
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
        if part.endswith("compresser"):
            return "compresser"
    return None


def _find_application(parts: list[str]) -> str | None:
    for index in range(len(parts) - 2, -1, -1):
        if parts[index] in _APPLICATION_MARKERS:
            return parts[index + 1]
    return None


def _application_scope(parts: list[str]) -> list[str]:
    for index in range(len(parts) - 2, -1, -1):
        if parts[index] in _APPLICATION_MARKERS:
            return parts[index + 2 :]
    return parts


def _find_layer(parts: list[str], common_roots: tuple[str, ...]) -> str | None:
    common = set(common_roots)
    for part in reversed(parts):
        if part in _LAYER_NAMES:
            return part
        if part in common:
            return "common"
    return None
