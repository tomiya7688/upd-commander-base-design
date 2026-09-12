import argparse
from pathlib import Path

from .scanner import scan_path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="upd-commander-check")
    parser.add_argument("target", nargs="?", default=".")
    parser.add_argument("--ignore", action="append", default=[], metavar="GLOB")
    parser.add_argument("--warnings-as-errors", action="store_true")
    return parser


def main() -> int:
    args = build_parser().parse_args()
    target = Path(args.target).resolve()

    if not target.exists():
        print(f"E UPD000 {target}: missing")
        return 2

    findings = scan_path(target, tuple(args.ignore))
    for finding in findings:
        level = "E" if finding.severity == "error" else "W"
        print(f"{level} {finding.code} {_display_path(finding.path, target)}:{finding.line} {finding.message}")

    error_count = sum(item.severity == "error" for item in findings)
    warning_count = sum(item.severity == "warning" for item in findings)
    if error_count or (warning_count and args.warnings_as_errors):
        print(f"FAIL e={error_count} w={warning_count}")
        return 1

    if warning_count:
        print(f"OK w={warning_count}")
    else:
        print("OK")
    return 0


def _display_path(path: Path, target: Path) -> str:
    root = target if target.is_dir() else target.parent
    try:
        return path.relative_to(root).as_posix()
    except ValueError:
        return path.as_posix()
