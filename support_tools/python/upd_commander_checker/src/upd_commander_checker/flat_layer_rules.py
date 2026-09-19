from fnmatch import fnmatch
from pathlib import Path

from .ignore_rules import IgnoreRule
from .models import Finding


_FLAT_LAYER_EXCLUDED_DIRS = {"generated", "third_party", "vendor", "external", "build"}
_APPLICATION_MARKERS = {"app", "apps", "application", "applications", "feature", "features"}
_LAYER_NAMES = {"ui", "process", "data"}


def check_flat_layers(
    paths: list[Path],
    root: Path,
    ignore_rules: tuple[IgnoreRule, ...],
    min_files: int,
    min_direct_percent: int,
) -> list[Finding]:
    counts: dict[str, list[int]] = {}
    for path in paths:
        relative = Path(_relative_text(path, root))
        parts = relative.parts
        lowered = tuple(part.lower() for part in parts)
        if any(part in _FLAT_LAYER_EXCLUDED_DIRS for part in lowered[:-1]):
            continue

        layer_index = _layer_root_index(lowered)
        if layer_index is None:
            continue
        layer_root = Path(*parts[: layer_index + 1]).as_posix()
        direct = len(parts) == layer_index + 2
        bucket = counts.setdefault(layer_root, [0, 0])
        bucket[0] += 1
        if direct:
            bucket[1] += 1

    findings: list[Finding] = []
    for layer_root, (total_files, direct_files) in counts.items():
        if total_files < min_files:
            continue
        if direct_files * 100 < total_files * min_direct_percent:
            continue
        if any(
            rule.code == "UPD405" and fnmatch(layer_root, rule.pattern)
            for rule in ignore_rules
        ):
            continue
        findings.append(
            Finding(
                Path(layer_root),
                1,
                "UPD405",
                "large flat layer reduces navigability; consider grouping related responsibilities",
                "attention",
            )
        )
    return findings


def _layer_root_index(parts: tuple[str, ...]) -> int | None:
    scope_start = 0
    for index in range(len(parts) - 2):
        if parts[index] in _APPLICATION_MARKERS:
            scope_start = index + 2

    layer_index = None
    for index in range(scope_start, len(parts) - 1):
        if parts[index] in _LAYER_NAMES:
            layer_index = index
    return layer_index


def _relative_text(path: Path, root: Path) -> str:
    try:
        return path.relative_to(root).as_posix()
    except ValueError:
        return path.as_posix()
