import ast
from fnmatch import fnmatch
import os
from pathlib import Path

from .classifier import classify_module
from .commander_rules import check_commander
from .container_rules import check_containers
from .data_type_location_rules import check_data_type_locations
from .dependency_rules import check_dependencies
from .flat_layer_rules import check_flat_layers
from .ignore_rules import IgnoreRule, filter_findings, is_path_ignored, load_ignore_rules
from .models import Finding
from .model_attention_rules import (
    ModelGroupOccurrence,
    collect_path_model_group_occurrences,
    model_attention_findings,
)
from .responsibility_rules import check_responsibilities


def scan_path(
    target: Path,
    ignore_patterns: tuple[str, ...] = (),
    upd301_max_inputs: int = 2,
    flat_layer_min_files: int = 12,
    flat_layer_min_direct_percent: int = 80,
    model_group_min_items: int = 3,
    model_group_min_occurrences: int = 2,
) -> list[Finding]:
    root = target if target.is_dir() else target.parent
    classification_root = root if target.is_dir() else None
    try:
        ignore_rules = load_ignore_rules(root)
    except (OSError, UnicodeError) as exc:
        return [Finding(Path(".updcommanderignore"), 1, "UPD001", f"read failed: {exc}")]

    findings: list[Finding] = []
    paths = _python_files(target, root, ignore_patterns, ignore_rules, findings)
    internal_modules = _internal_module_names(paths, root)
    model_occurrences: list[ModelGroupOccurrence] = []
    for path in paths:
        findings.extend(
            _scan_file(
                path,
                root,
                classification_root,
                ignore_rules,
                internal_modules,
                upd301_max_inputs,
            )
        )
        model_occurrences.extend(
            collect_path_model_group_occurrences(
                path,
                root,
                classification_root,
                ignore_rules,
                model_group_min_items,
            )
        )
    findings.extend(
        _filter_location_findings(check_data_type_locations(paths, root), root, ignore_rules)
    )
    findings.extend(model_attention_findings(model_occurrences, model_group_min_occurrences))
    if target.is_dir():
        findings.extend(
            check_flat_layers(
                paths,
                root,
                ignore_rules,
                flat_layer_min_files,
                flat_layer_min_direct_percent,
            )
        )
    return sorted(findings, key=lambda item: (str(item.path), item.line, item.code))


def _python_files(
    target: Path,
    root: Path,
    ignore_patterns: tuple[str, ...],
    ignore_rules: tuple[IgnoreRule, ...],
    findings: list[Finding],
) -> list[Path]:
    if target.is_file():
        paths = [target]
    else:
        paths = []

        def on_error(error: OSError) -> None:
            error_path = Path(error.filename) if error.filename else target
            findings.append(
                Finding(
                    Path(_relative_text(error_path, root)),
                    1,
                    "UPD001",
                    f"read failed: {error}",
                )
            )

        for directory, _, names in os.walk(target, onerror=on_error):
            paths.extend(
                Path(directory) / name
                for name in names
                if name.endswith(".py")
            )

    return [
        path
        for path in paths
        if not _is_ignored(path, root, ignore_patterns, ignore_rules)
    ]


def _internal_module_names(paths: list[Path], root: Path) -> frozenset[str]:
    names: set[str] = set()
    for path in paths:
        names.add(_module_name(path, None))
        names.add(_module_name(path, root))
    return frozenset(name for name in names if name)


def _module_name(path: Path, root: Path | None) -> str:
    module_path = path
    if root is not None:
        try:
            module_path = path.relative_to(root)
        except ValueError:
            pass
    parts = [part for part in module_path.with_suffix("").parts if part != module_path.anchor]
    if parts and parts[-1] == "__init__":
        parts.pop()
    return ".".join(parts)


def _is_ignored(
    path: Path,
    root: Path,
    patterns: tuple[str, ...],
    ignore_rules: tuple[IgnoreRule, ...],
) -> bool:
    relative_text = _relative_text(path, root)
    if any(fnmatch(relative_text, pattern) for pattern in patterns):
        return True
    return is_path_ignored(relative_text, ignore_rules)


def _scan_file(
    path: Path,
    root: Path,
    classification_root: Path | None,
    ignore_rules: tuple[IgnoreRule, ...],
    internal_modules: frozenset[str],
    upd301_max_inputs: int,
) -> list[Finding]:
    try:
        source = path.read_text(encoding="utf-8")
        tree = ast.parse(source, filename=str(path))
    except (OSError, UnicodeError) as exc:
        return [Finding(path, 1, "UPD001", f"read failed: {exc}")]
    except SyntaxError as exc:
        return [Finding(path, exc.lineno or 1, "UPD002", f"syntax: {exc.msg}")]

    module = classify_module(path, classification_root)
    findings = check_dependencies(tree, module, internal_modules)
    findings.extend(check_commander(tree, module))
    findings.extend(check_containers(tree, module, upd301_max_inputs))
    findings.extend(check_responsibilities(tree, module))
    return filter_findings(findings, source, _relative_text(path, root), ignore_rules)


def _filter_location_findings(
    findings: list[Finding],
    root: Path,
    ignore_rules: tuple[IgnoreRule, ...],
) -> list[Finding]:
    filtered: list[Finding] = []
    for finding in findings:
        source_path = root / finding.path
        try:
            source = source_path.read_text(encoding="utf-8")
        except (OSError, UnicodeError):
            continue
        filtered.extend(
            filter_findings([finding], source, finding.path.as_posix(), ignore_rules)
        )
    return filtered


def _relative_text(path: Path, root: Path) -> str:
    try:
        return path.relative_to(root).as_posix()
    except ValueError:
        return path.as_posix()


