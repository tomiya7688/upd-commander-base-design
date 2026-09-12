import ast
from fnmatch import fnmatch
from pathlib import Path

from .classifier import classify_module
from .commander_rules import check_commander
from .dependency_rules import check_dependencies
from .models import Finding
from .suppressions import filter_suppressed


def scan_path(target: Path, ignore_patterns: tuple[str, ...] = ()) -> list[Finding]:
    findings: list[Finding] = []
    paths = [target] if target.is_file() else list(target.rglob("*.py"))
    for path in paths:
        if _matches_ignore(path, target, ignore_patterns):
            continue
        findings.extend(_scan_file(path))
    return sorted(findings, key=lambda item: (str(item.path), item.line, item.code))


def _matches_ignore(path: Path, target: Path, patterns: tuple[str, ...]) -> bool:
    root = target if target.is_dir() else target.parent
    try:
        relative = path.relative_to(root).as_posix()
    except ValueError:
        relative = path.as_posix()
    return any(fnmatch(relative, pattern) for pattern in patterns)


def _scan_file(path: Path) -> list[Finding]:
    try:
        source = path.read_text(encoding="utf-8")
        tree = ast.parse(source, filename=str(path))
    except (OSError, UnicodeError) as exc:
        return [Finding(path, 1, "UPD001", f"read failed: {exc}")]
    except SyntaxError as exc:
        return [Finding(path, exc.lineno or 1, "UPD002", f"syntax: {exc.msg}")]

    module = classify_module(path)
    findings = check_dependencies(tree, module)
    findings.extend(check_commander(tree, module))
    return filter_suppressed(source, findings)
