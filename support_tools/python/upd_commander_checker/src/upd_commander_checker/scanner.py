import ast
from fnmatch import fnmatch
from pathlib import Path

from .classifier import classify_module
from .commander_rules import check_commander
from .container_rules import check_containers
from .dependency_rules import check_dependencies
from .ignore_rules import IgnoreRule, filter_findings, is_path_ignored, load_ignore_rules
from .models import Finding
from .responsibility_rules import check_responsibilities


def scan_path(target: Path, ignore_patterns: tuple[str, ...] = ()) -> list[Finding]:
    root = target if target.is_dir() else target.parent
    classification_root = root if target.is_dir() else None
    ignore_rules = load_ignore_rules(root)
    findings: list[Finding] = []
    for path in _python_files(target, root, ignore_patterns, ignore_rules):
        findings.extend(_scan_file(path, root, classification_root, ignore_rules))
    return sorted(findings, key=lambda item: (str(item.path), item.line, item.code))


def _python_files(
    target: Path,
    root: Path,
    ignore_patterns: tuple[str, ...],
    ignore_rules: tuple[IgnoreRule, ...],
) -> list[Path]:
    paths = [target] if target.is_file() else list(target.rglob("*.py"))
    return [
        path
        for path in paths
        if not _is_ignored(path, root, ignore_patterns, ignore_rules)
    ]


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
) -> list[Finding]:
    try:
        source = path.read_text(encoding="utf-8")
        tree = ast.parse(source, filename=str(path))
    except (OSError, UnicodeError) as exc:
        return [Finding(path, 1, "UPD001", f"read failed: {exc}")]
    except SyntaxError as exc:
        return [Finding(path, exc.lineno or 1, "UPD002", f"syntax: {exc.msg}")]

    module = classify_module(path, classification_root)
    findings = check_dependencies(tree, module)
    findings.extend(check_commander(tree, module))
    findings.extend(check_containers(tree, module))
    findings.extend(check_responsibilities(tree, module))
    return filter_findings(findings, source, _relative_text(path, root), ignore_rules)


def _relative_text(path: Path, root: Path) -> str:
    try:
        return path.relative_to(root).as_posix()
    except ValueError:
        return path.as_posix()
