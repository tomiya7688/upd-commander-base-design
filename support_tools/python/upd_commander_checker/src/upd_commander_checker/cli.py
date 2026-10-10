import argparse
from pathlib import Path

from .config import ConfigError, load_config
from .gate_policy import (
    apply_severity_overrides,
    parse_fail_on_argument,
    parse_severity_override_argument,
    should_fail,
)
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
    parser.add_argument("--fail-on", default=None, metavar="SEVERITIES")
    parser.add_argument(
        "--severity-override", action="append", default=[], metavar="RULE=SEVERITY"
    )
    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()
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
    explicit_gate = args.fail_on is not None or config.fail_on is not None
    try:
        fail_on = (
            parse_fail_on_argument(args.fail_on)
            if args.fail_on is not None
            else config.fail_on
        )
        overrides = dict(config.severity_overrides)
        for item in args.severity_override:
            rule, severity = parse_severity_override_argument(item)
            overrides[rule] = severity
    except ValueError as exc:
        parser.error(str(exc))

    if not target.exists():
        return finish_report([f"E UPD000 {target}: missing"], output, 2)

    findings = filter_enabled_findings(
        scan_path(
            target,
            ignores,
            config.upd301_max_inputs,
            config.flat_layer_min_files,
            config.flat_layer_min_direct_percent,
            config.model_group_min_items,
            config.model_group_min_occurrences,
            config.common_roots,
        ),
        config.enabled_rules,
    )
    findings = apply_severity_overrides(findings, overrides)
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
    if not explicit_gate:
        legacy_fail_on = ["error"]
        if warnings_as_errors:
            legacy_fail_on.append("warning")
        if attentions_as_errors:
            legacy_fail_on.append("attention")
        fail_on = tuple(legacy_fail_on)
    failed = should_fail(findings, fail_on or ())
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
