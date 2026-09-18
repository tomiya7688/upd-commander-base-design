import argparse
from pathlib import Path

from .config import ConfigError, load_config
from .report_output import finish_report
from .rule_selection import filter_enabled_findings
from .scanner import scan_path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="upd-commander-check")
    parser.add_argument("target", nargs="?", default=None)
    parser.add_argument("--output", default=None)
    parser.add_argument("--ignore", action="append", default=[], metavar="GLOB")
    parser.add_argument("--warnings-as-errors", action="store_true")
    parser.add_argument("--attentions-as-errors", action="store_true")
    return parser


def main() -> int:
    args = build_parser().parse_args()
    try:
        config = load_config()
    except ConfigError as exc:
        print(f"CONFIG ERROR: {exc}")
        return 2
    target = Path(args.target or config.input_path).resolve()
    output = args.output if args.output is not None else config.output_path
    ignores = tuple(config.ignore) + tuple(args.ignore)
    warnings_as_errors = config.warnings_as_errors or args.warnings_as_errors
    attentions_as_errors = args.attentions_as_errors

    if not target.exists():
        return finish_report([f"E UPD000 {target}: missing"], output, 2)

    findings = filter_enabled_findings(
        scan_path(
            target,
            ignores,
            config.upd301_max_inputs,
            config.flat_layer_min_files,
            config.flat_layer_min_direct_percent,
        ),
        config.enabled_rules,
    )
    lines = []
    for finding in findings:
        level = {"error": "E", "warning": "W", "attention": "A"}.get(
            finding.severity, "A"
        )
        lines.append(
            f"{level} {finding.code} {_display_path(finding.path, target)}:{finding.line} {finding.message}"
        )

    error_count = sum(item.severity == "error" for item in findings)
    warning_count = sum(item.severity == "warning" for item in findings)
    attention_count = sum(item.severity == "attention" for item in findings)
    failed = (
        error_count > 0
        or warnings_as_errors and warning_count > 0
        or attentions_as_errors and attention_count > 0
    )
    if failed:
        lines.append(f"FAIL e={error_count} w={warning_count} a={attention_count}")
        return finish_report(lines, output, 1)

    if warning_count or attention_count:
        lines.append(f"OK w={warning_count} a={attention_count}")
    else:
        lines.append("OK")
    return finish_report(lines, output, 0)


def _display_path(path: Path, target: Path) -> str:
    root = target if target.is_dir() else target.parent
    try:
        return path.relative_to(root).as_posix()
    except ValueError:
        return path.as_posix()
