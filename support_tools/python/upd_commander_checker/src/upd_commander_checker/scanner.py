import ast
from fnmatch import fnmatch
from pathlib import Path

from .classifier import classify_module
from .commander_rules import check_commander
from .dependency_rules import check_dependencies
from .models import Finding


def scan_path(target: Path, ignore_patterns: tuple[str, ...] = ()) -> list[Finding]:
    findings: list[Finding] = []
    for path in _python_files(target, ignore_patterns):
        findings.extend(_scan_file(path))
    return sorted(findings, key=lambda item: (str(item.path), item.line, item.code))


def _python_files(target: Path, ignore_patterns: tuple[str, ...]) -> list[Path]:
    paths = [target] if target.is_file() else list(target.rglob("*.py"))
    return [path for path in paths if not _is_ignored(path, target, ignore_patterns)]


def _is_ignored(path: Path, target: Path, patterns: tuple[str, ...]) -> bool:
    try:
        relative = path.relative_to(target if target.is_dir() else target.parent)
    except ValueError:
        relative = path
    relative_text = relative.as_posix()
    return any(fnmatch(relative_text, pattern) for pattern in patterns)


def _scan_file(path: Path) -> list[Finding]:
    try:
        source = path.read_text(encoding="utf-8")
        tree = ast.parse(source, filename=str(path))
    except (OSError, UnicodeError) as exc:
        return [Finding(path, 1, "UPD001", f"Cannot read Python source: {exc}")]
    except SyntaxError as exc:
        return [Finding(path, exc.lineno or 1, "UPD002", f"Python syntax error: {exc.msg}")]

    module = classify_module(path)
    findings = check_dependencies(tree, module)
    findings.extend(check_commander(tree, module))
    return findings
