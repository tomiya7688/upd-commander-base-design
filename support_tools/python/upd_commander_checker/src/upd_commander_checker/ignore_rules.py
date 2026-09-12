from dataclasses import dataclass
from fnmatch import fnmatch
from pathlib import Path
import re

from .models import Finding


_INLINE_IGNORE = re.compile(r"#\s*upd:\s*ignore\s+(UPD\d+|all)\b", re.IGNORECASE)


@dataclass(frozen=True)
class IgnoreRule:
    pattern: str
    code: str | None = None


def load_ignore_rules(root: Path) -> tuple[IgnoreRule, ...]:
    path = root / ".updcommanderignore"
    if not path.is_file():
        return ()

    rules: list[IgnoreRule] = []
    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.split("#", 1)[0].strip()
        if not line:
            continue
        parts = line.split(maxsplit=1)
        if len(parts) == 2 and parts[0].upper().startswith("UPD"):
            rules.append(IgnoreRule(parts[1], parts[0].upper()))
        else:
            rules.append(IgnoreRule(line))
    return tuple(rules)


def is_path_ignored(relative_path: str, rules: tuple[IgnoreRule, ...]) -> bool:
    return any(rule.code is None and fnmatch(relative_path, rule.pattern) for rule in rules)


def filter_findings(
    findings: list[Finding],
    source: str,
    relative_path: str,
    rules: tuple[IgnoreRule, ...],
) -> list[Finding]:
    lines = source.splitlines()
    return [
        finding
        for finding in findings
        if not _finding_ignored(finding, lines, relative_path, rules)
    ]


def _finding_ignored(
    finding: Finding,
    lines: list[str],
    relative_path: str,
    rules: tuple[IgnoreRule, ...],
) -> bool:
    if any(
        rule.code == finding.code and fnmatch(relative_path, rule.pattern)
        for rule in rules
    ):
        return True

    if not 1 <= finding.line <= len(lines):
        return False
    match = _INLINE_IGNORE.search(lines[finding.line - 1])
    if not match:
        return False
    code = match.group(1).upper()
    return code == "ALL" or code == finding.code
