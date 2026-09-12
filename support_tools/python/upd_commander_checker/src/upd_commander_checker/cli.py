import argparse
from pathlib import Path

from .scanner import scan_path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="upd-commander-check",
        description="Check Python source against mechanically verifiable UPD Commander rules.",
    )
    parser.add_argument("target", nargs="?", default=".", help="Python file or project directory")
    parser.add_argument(
        "--ignore",
        action="append",
        default=[],
        metavar="GLOB",
        help="Ignore a relative path glob. Can be specified multiple times.",
    )
    parser.add_argument(
        "--warnings-as-errors",
        action="store_true",
        help="Return failure when warnings are found.",
    )
    return parser


def main() -> int:
    args = build_parser().parse_args()
    target = Path(args.target).resolve()

    if not target.exists():
        print(f"ERROR UPD000 {target}: target does not exist")
        return 2

    findings = scan_path(target, tuple(args.ignore))
    for finding in findings:
        print(
            f"{finding.severity.upper()} {finding.code} "
            f"{finding.path}:{finding.line}: {finding.message}"
        )

    error_count = sum(item.severity == "error" for item in findings)
    warning_count = sum(item.severity == "warning" for item in findings)
    print(f"UPD Commander check: {error_count} error(s), {warning_count} warning(s)")

    if error_count:
        return 1
    if warning_count and args.warnings_as_errors:
        return 1
    return 0
