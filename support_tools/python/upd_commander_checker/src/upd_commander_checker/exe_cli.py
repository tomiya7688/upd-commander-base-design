import argparse
from pathlib import Path

from .scanner import scan_path


def main() -> int:
    parser = argparse.ArgumentParser(prog="upd-commander-check")
    parser.add_argument("target", nargs="?", default=".")
    parser.add_argument("--ignore", action="append", default=[])
    parser.add_argument("--warnings-as-errors", action="store_true")
    args = parser.parse_args()

    target = Path(args.target).resolve()
    if not target.exists():
        print("E UPD000 missing")
        return 2

    findings = scan_path(target, tuple(args.ignore))
    for finding in findings:
        level = "E" if finding.severity == "error" else "W"
        print(f"{level} {finding.code} {finding.path.name}:{finding.line}")

    errors = sum(item.severity == "error" for item in findings)
    warnings = sum(item.severity == "warning" for item in findings)
    print(f"{errors}E {warnings}W")
    return 1 if errors or (warnings and args.warnings_as_errors) else 0
