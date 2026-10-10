import argparse
from pathlib import Path

from .baseline import (
    BaselineError,
    compare_findings,
    finding_fingerprint,
    load_baseline,
    write_baseline,
)
from .config import ConfigError, load_config
from .gate_policy import (
    apply_severity_overrides,
    exception_reason,
    parse_fail_on_argument,
    parse_severity_override_argument,
    should_fail,
)
from .report_output import finish_report
from .rule_selection import filter_enabled_findings
from .scanner import scan_path


# {
# 責務: [build_parser: Checker CLIの位置引数とoptionを定義する]
# 処理: [1: target/output/ignore optionを登録する, 2: baselineとgate optionを登録する]
# 引数: [なし]
# 戻り値: [設定済みArgumentParser]
# }
def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="upd-commander-check")
    parser.add_argument("target", nargs="?", default=None)
    parser.add_argument("--output", default=None)
    parser.add_argument("--ignore", action="append", default=[], metavar="GLOB")
    parser.add_argument("--warnings-as-errors", action="store_true")
    parser.add_argument("--attentions-as-errors", action="store_true")
    baseline_group = parser.add_mutually_exclusive_group()
    baseline_group.add_argument("--write-baseline", nargs="?", const="", metavar="PATH")
    baseline_group.add_argument("--baseline", metavar="PATH")
    parser.add_argument("--fail-on", default=None, metavar="SEVERITIES")
    parser.add_argument(
        "--severity-override", action="append", default=[], metavar="RULE=SEVERITY"
    )
    return parser


# {
# 責務: [main: scan・baseline比較・gate判定を実行して報告する]
# 処理: [1: configとCLIを検証する, 2: Findingをscan・分類する, 3: gate結果を出力する]
# 引数: [なし]
# 戻り値: [成功0、Finding gate失敗1、設定・実行エラー2]
# }
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
    finding_records = _baseline_records(findings, target)
    statuses: dict[str, str] = {}
    resolved: list[dict[str, object]] = []
    try:
        if args.write_baseline is not None:
            baseline_path = (
                Path(args.write_baseline)
                if args.write_baseline
                else _scan_root(target) / ".upd-baseline.json"
            )
            write_baseline(baseline_path, finding_records)
        elif args.baseline is not None:
            comparison = compare_findings(
                finding_records, load_baseline(Path(args.baseline))
            )
            statuses = {
                str(entry["fingerprint"]): str(entry["status"])
                for group in (comparison["new"], comparison["existing"])
                for entry in group
            }
            resolved = comparison["resolved"]
    except (BaselineError, OSError, ValueError) as exc:
        print(f"BASELINE ERROR: {exc}")
        return 2
    lines = []
    for finding in findings:
        level = {"error": "E", "warning": "W", "attention": "A"}.get(
            finding.severity, "A"
        )
        line = (
            f"{level} {finding.code} {_display_path(finding.path, target)}:"
            f"{finding.line} {finding.message}"
        )
        display_path = _display_path(finding.path, target)
        reason = exception_reason(finding, display_path, config.gate_exceptions)
        suffix = f" [gate exception: {reason}]" if reason is not None else ""
        if args.baseline is not None:
            identity = finding_fingerprint(
                finding.code,
                _display_path(finding.path, target),
                finding.symbol,
                finding.context,
            )
            line = f"{statuses[identity]} {line}"
        lines.append(line + suffix)

    for entry in resolved:
        level = {"error": "E", "warning": "W", "attention": "A"}.get(
            str(entry["severity"]), "A"
        )
        message = str(entry.get("message", ""))
        line_number = entry.get("line", "?")
        lines.append(
            f"RESOLVED {level} {entry['rule']} {entry['path']}:{line_number} {message}".rstrip()
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
    gate_findings = [
        finding
        for finding in findings
        if exception_reason(
            finding, _display_path(finding.path, target), config.gate_exceptions
        )
        is None
    ]
    failed = should_fail(gate_findings, fail_on or ())
    if failed:
        lines.append(f"FAIL e={error_count} w={warning_count} a={attention_count}")
        return finish_report(lines, output, 1)

    if warning_count or attention_count:
        lines.append(f"OK w={warning_count} a={attention_count}")
    else:
        lines.append("OK")
    return finish_report(lines, output, 0)


# {
# 責務: [_display_path: Finding pathをscan root相対の表示形式へ変換する]
# 処理: [1: scan rootを決める, 2: 相対path化し、範囲外なら元pathを使う]
# 引数: [path: Finding path, target: scan target]
# 戻り値: [表示用path]
# }
def _display_path(path: Path, target: Path) -> str:
    root = _scan_root(target)
    try:
        return path.resolve().relative_to(root.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


# {
# 責務: [_scan_root: targetがfileかdirectoryかに応じscan rootを返す]
# 処理: [1: directoryならtargetを返す, 2: fileなら親directoryを返す]
# 引数: [target: scan target]
# 戻り値: [scan root]
# }
def _scan_root(target: Path) -> Path:
    return target if target.is_dir() else target.parent


# {
# 責務: [_baseline_records: Findingをbaseline書込用のidentity recordへ変換する]
# 処理: [1: identityと表示metadataを抽出する, 2: pathをscan root相対にする]
# 引数: [findings: scan結果, target: scan target]
# 戻り値: [baseline entry辞書一覧]
# }
def _baseline_records(findings: list, target: Path) -> list[dict[str, object]]:
    records: list[dict[str, object]] = []
    for finding in findings:
        records.append(
            {
                "rule": finding.code,
                "path": _display_path(finding.path, target),
                "symbol": finding.symbol,
                "context": finding.context,
                "severity": finding.severity,
                "line": finding.line,
                "message": finding.message,
            }
        )
    return records
