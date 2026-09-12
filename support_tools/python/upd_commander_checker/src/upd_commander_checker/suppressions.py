import re

from .models import Finding

_IGNORE_PATTERN = re.compile(
    r"#\s*upd-ignore:\s*(?P<codes>[A-Z0-9*, ]+)\s+reason=(?P<reason>.+)$"
)
_IGNORE_FILE_PATTERN = re.compile(
    r"#\s*upd-ignore-file:\s*(?P<codes>[A-Z0-9*, ]+)\s+reason=(?P<reason>.+)$"
)


def filter_suppressed(source: str, findings: list[Finding]) -> list[Finding]:
    lines = source.splitlines()
    file_codes = _file_codes(lines)
    line_codes = _line_codes(lines)

    return [
        finding
        for finding in findings
        if not _is_suppressed(finding, file_codes, line_codes)
    ]


def _file_codes(lines: list[str]) -> set[str]:
    codes: set[str] = set()
    for line in lines:
        match = _IGNORE_FILE_PATTERN.search(line)
        if match:
            codes.update(_parse_codes(match.group("codes")))
    return codes


def _line_codes(lines: list[str]) -> dict[int, set[str]]:
    suppressions: dict[int, set[str]] = {}
    for index, line in enumerate(lines, start=1):
        match = _IGNORE_PATTERN.search(line)
        if not match:
            continue
        codes = _parse_codes(match.group("codes"))
        suppressions.setdefault(index, set()).update(codes)
        suppressions.setdefault(index + 1, set()).update(codes)
    return suppressions


def _parse_codes(text: str) -> set[str]:
    return {code.strip() for code in text.split(",") if code.strip()}


def _is_suppressed(
    finding: Finding,
    file_codes: set[str],
    line_codes: dict[int, set[str]],
) -> bool:
    if "*" in file_codes or finding.code in file_codes:
        return True

    codes = line_codes.get(finding.line, set())
    return "*" in codes or finding.code in codes
