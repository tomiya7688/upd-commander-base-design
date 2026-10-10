"""Versioned Finding identity and baseline serialization helpers."""

from __future__ import annotations

from collections.abc import Iterable, Mapping
import hashlib
import json
import re
import unicodedata
from pathlib import Path, PurePosixPath
from typing import Any


SCHEMA_VERSION = 1
FINGERPRINT_VERSION = 1
_DOMAIN_SEPARATOR = "upd-finding-fingerprint-v1"
_RULE_PATTERN = re.compile(r"UPD[0-9]{3,}")
_FINGERPRINT_PATTERN = re.compile(r"sha256:[0-9a-f]{64}")
_REQUIRED_ENTRY_FIELDS = {
    "fingerprint",
    "rule",
    "path",
    "symbol",
    "context",
    "severity",
}


# {
# 責務: [BaselineError: baselineの不正やFinding identityの曖昧さを通知する]
# フィールド: [args: ValueErrorから継承する診断情報]
# 処理: [1: 検証失敗の理由を例外として保持する]
# }
class BaselineError(ValueError):
    """baselineの不正やFinding identityの曖昧さを表す。"""


# {
# 責務: [finding_fingerprint: Finding identityから安定したv1 fingerprintを生成する]
# 処理: [1: rule/path/symbol/contextを正規化する, 2: NUL区切りのSHA-256を計算する]
# 引数: [rule/path/symbol/context: Finding identityを構成する値]
# 戻り値: [sha256:形式の小文字hex fingerprint]
# エラー: [rule・path・contextが契約に反する場合]
# }
def finding_fingerprint(
    rule: str, path: str, symbol: str, context: str
) -> str:
    """正規化したidentity fieldからversion 1 fingerprintを返す。"""
    canonical_rule = unicodedata.normalize("NFC", rule).upper()
    if _RULE_PATTERN.fullmatch(canonical_rule) is None:
        raise BaselineError("rule must match UPD followed by at least three digits")

    canonical_path = unicodedata.normalize("NFC", path.replace("\\", "/"))
    parsed_path = PurePosixPath(canonical_path)
    if (
        parsed_path.is_absolute()
        or re.match(r"^[A-Za-z]:", canonical_path)
        or canonical_path.startswith("//")
        or ".." in parsed_path.parts
    ):
        raise BaselineError("path must be repository-relative")
    canonical_path = "/".join(
        part for part in parsed_path.parts if part not in {"", "."}
    )
    if not canonical_path:
        raise BaselineError("path must not be empty")

    canonical_symbol = unicodedata.normalize("NFC", symbol)
    canonical_context = unicodedata.normalize("NFC", context)
    fields = (
        _DOMAIN_SEPARATOR,
        canonical_rule,
        canonical_path,
        canonical_symbol,
        canonical_context,
    )
    if not canonical_context:
        raise BaselineError("context must not be empty")
    if any("\0" in field for field in fields):
        raise BaselineError("identity fields must not contain NUL")
    payload = "\0".join(fields).encode("utf-8")
    return "sha256:" + hashlib.sha256(payload).hexdigest()


# {
# 責務: [build_baseline: Finding一覧から決定的なversion 1 baselineを構築する]
# 処理: [1: Findingをentryへ変換する, 2: fingerprintの一意性を検証する, 3: 順序を固定する]
# 引数: [findings: baselineへ記録するFinding一覧]
# 戻り値: [schema_version等を含むbaseline辞書]
# エラー: [identityが不正または重複する場合]
# }
def build_baseline(findings: Iterable[Mapping[str, Any]]) -> dict[str, Any]:
    """Finding一覧から決定的なversion 1 baselineを構築する。"""
    entries: list[dict[str, Any]] = []
    seen: set[str] = set()
    for finding in findings:
        entry = _entry_from_finding(finding)
        fingerprint = entry["fingerprint"]
        if fingerprint in seen:
            raise BaselineError(
                f"duplicate Finding identity in scan: {fingerprint}"
            )
        seen.add(fingerprint)
        entries.append(entry)
    entries.sort(key=lambda item: item["fingerprint"])
    return {
        "schema_version": SCHEMA_VERSION,
        "fingerprint_version": FINGERPRINT_VERSION,
        "findings": entries,
    }


# {
# 責務: [write_baseline: 検証済みbaselineをJSONファイルへ保存する]
# 処理: [1: baselineを構築する, 2: 親directoryを作る, 3: UTF-8 JSONを書き込む]
# 引数: [path: 保存先, findings: 記録するFinding一覧]
# 戻り値: [なし]
# 副作用: [baselineファイルと必要な親directoryを作成する]
# }
def write_baseline(path: Path, findings: Iterable[Mapping[str, Any]]) -> None:
    """検証済みbaselineをJSONファイルへ保存する。"""
    baseline = build_baseline(findings)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps(baseline, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


# {
# 責務: [load_baseline: JSONファイルを読み込みversion 1 baselineとして検証する]
# 処理: [1: UTF-8 JSONを読む, 2: 構造とidentityを検証する]
# 引数: [path: 読み込むbaselineファイル]
# 戻り値: [検証・正規化済みbaseline辞書]
# エラー: [読込・JSON解析・契約検証に失敗した場合]
# }
def load_baseline(path: Path) -> dict[str, Any]:
    """JSONファイルを読み込みversion 1 baselineとして検証する。"""
    try:
        document = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise BaselineError(f"cannot read baseline: {exc}") from exc
    return validate_baseline(document)


# {
# 責務: [validate_baseline: baseline v1のschemaとFinding identityを検証する]
# 処理: [1: versionと必須fieldを確認する, 2: entryを正規化する, 3: fingerprintと一意性を確認する]
# 引数: [document: 検証対象のJSON値]
# 戻り値: [検証・正規化済みbaseline辞書]
# エラー: [型・version・path・fingerprint等が契約に反する場合]
# }
def validate_baseline(document: Any) -> dict[str, Any]:
    if not isinstance(document, dict):
        raise BaselineError("baseline root must be an object")
    schema_version = document.get("schema_version")
    fingerprint_version = document.get("fingerprint_version")
    if type(schema_version) is not int or schema_version != SCHEMA_VERSION:
        raise BaselineError(
            f"unsupported schema_version {schema_version!r}; supported version is {SCHEMA_VERSION}"
        )
    if (
        type(fingerprint_version) is not int
        or fingerprint_version != FINGERPRINT_VERSION
    ):
        raise BaselineError(
            "unsupported fingerprint_version "
            f"{fingerprint_version!r}; supported version is {FINGERPRINT_VERSION}"
        )
    findings = document.get("findings")
    if not isinstance(findings, list):
        raise BaselineError("findings must be an array")

    validated: list[dict[str, Any]] = []
    seen: set[str] = set()
    for index, finding in enumerate(findings):
        if not isinstance(finding, dict):
            raise BaselineError(f"findings[{index}] must be an object")
        missing = _REQUIRED_ENTRY_FIELDS - finding.keys()
        if missing:
            raise BaselineError(
                f"findings[{index}] is missing required fields: {', '.join(sorted(missing))}"
            )
        rule = _string_field(finding, "rule", index)
        path = _string_field(finding, "path", index)
        symbol = _string_field(finding, "symbol", index)
        context = _string_field(finding, "context", index)
        severity = _string_field(finding, "severity", index)
        fingerprint = _string_field(finding, "fingerprint", index)
        if severity not in {"error", "warning", "attention"}:
            raise BaselineError(f"findings[{index}].severity is invalid")
        if _FINGERPRINT_PATTERN.fullmatch(fingerprint) is None:
            raise BaselineError(f"findings[{index}].fingerprint is invalid")
        expected = finding_fingerprint(rule, path, symbol, context)
        if fingerprint != expected:
            raise BaselineError(f"findings[{index}].fingerprint does not match identity")
        if fingerprint in seen:
            raise BaselineError(f"duplicate fingerprint: {fingerprint}")
        seen.add(fingerprint)

        entry = {
            "fingerprint": fingerprint,
            "rule": rule,
            "path": path,
            "symbol": symbol,
            "context": context,
            "severity": severity,
        }
        if "line" in finding:
            line = finding["line"]
            if type(line) is not int or line < 1:
                raise BaselineError(f"findings[{index}].line must be a positive integer")
            entry["line"] = line
        if "message" in finding:
            entry["message"] = _string_field(finding, "message", index)
        validated.append(entry)

    return {
        "schema_version": schema_version,
        "fingerprint_version": fingerprint_version,
        "findings": validated,
    }


# {
# 責務: [compare_findings: 現在とbaselineのFindingをNEW/EXISTING/RESOLVEDへ分類する]
# 処理: [1: baselineと現在のFindingを正規化する, 2: fingerprint集合を比較する]
# 引数: [current: 現在のFinding一覧, baseline: 比較するbaseline]
# 戻り値: [分類別のFinding辞書]
# エラー: [baselineまたは現在のidentityが不正・重複する場合]
# }
def compare_findings(
    current: Iterable[Mapping[str, Any]], baseline: Mapping[str, Any]
) -> dict[str, list[dict[str, Any]]]:
    """Findingを分類し、通常の出力対象からは除外しない。"""
    validated = validate_baseline(dict(baseline))
    current_by_fingerprint: dict[str, dict[str, Any]] = {}
    for finding in current:
        entry = _entry_from_finding(finding)
        fingerprint = entry["fingerprint"]
        if fingerprint in current_by_fingerprint:
            raise BaselineError(f"duplicate Finding identity in scan: {fingerprint}")
        current_by_fingerprint[fingerprint] = entry

    baseline_by_fingerprint = {
        entry["fingerprint"]: entry for entry in validated["findings"]
    }
    new = [
        _with_status(entry, "NEW")
        for fingerprint, entry in current_by_fingerprint.items()
        if fingerprint not in baseline_by_fingerprint
    ]
    existing = [
        _with_status(current_by_fingerprint[fingerprint], "EXISTING")
        for fingerprint in current_by_fingerprint.keys() & baseline_by_fingerprint.keys()
    ]
    resolved = [
        _with_status(entry, "RESOLVED")
        for fingerprint, entry in baseline_by_fingerprint.items()
        if fingerprint not in current_by_fingerprint
    ]
    for group in (new, existing, resolved):
        group.sort(key=lambda entry: entry["fingerprint"])
    return {"new": new, "existing": existing, "resolved": resolved}


# {
# 責務: [_entry_from_finding: Findingをcanonical baseline entryへ変換する]
# 処理: [1: 必須fieldを確認する, 2: identityを正規化する, 3: fingerprintとmetadataを作る]
# 引数: [finding: 変換するFinding]
# 戻り値: [baseline schemaのentry辞書]
# エラー: [必須field欠落または値の型が不正な場合]
# }
def _entry_from_finding(finding: Mapping[str, Any]) -> dict[str, Any]:
    try:
        rule = finding["rule"]
        path = finding["path"]
        symbol = finding["symbol"]
        context = finding["context"]
        severity = finding["severity"]
    except KeyError as exc:
        raise BaselineError(f"Finding is missing required identity field: {exc.args[0]}") from exc
    for name, value in (
        ("rule", rule),
        ("path", path),
        ("symbol", symbol),
        ("context", context),
        ("severity", severity),
    ):
        if not isinstance(value, str):
            raise BaselineError(f"Finding {name} must be a string")
    fingerprint = finding_fingerprint(rule, path, symbol, context)
    entry: dict[str, Any] = {
        "fingerprint": fingerprint,
        "rule": unicodedata.normalize("NFC", rule).upper(),
        "path": _canonical_path(path),
        "symbol": unicodedata.normalize("NFC", symbol),
        "context": unicodedata.normalize("NFC", context),
        "severity": severity,
    }
    if severity not in {"error", "warning", "attention"}:
        raise BaselineError("Finding severity is invalid")
    if "line" in finding:
        line = finding["line"]
        if type(line) is not int or line < 1:
            raise BaselineError("Finding line must be a positive integer")
        entry["line"] = line
    if "message" in finding:
        message = finding["message"]
        if not isinstance(message, str):
            raise BaselineError("Finding message must be a string")
        entry["message"] = message
    return entry


# {
# 責務: [_canonical_path: repository相対pathをslash区切りのcanonical表記にする]
# 処理: [1: Unicodeと区切りを正規化する, 2: 空要素とdot要素を整理する]
# 引数: [path: 正規化対象のpath]
# 戻り値: [canonical path]
# }
def _canonical_path(path: str) -> str:
    normalized = unicodedata.normalize("NFC", path.replace("\\", "/"))
    parsed = PurePosixPath(normalized)
    return "/".join(part for part in parsed.parts if part not in {"", "."})


# {
# 責務: [_string_field: Findingから必須文字列fieldを取得する]
# 処理: [1: fieldの型を確認する]
# 引数: [finding: 読み取り元, name: field名, index: Finding位置]
# 戻り値: [取得した文字列]
# エラー: [fieldが文字列でない場合]
# }
def _string_field(finding: Mapping[str, Any], name: str, index: int) -> str:
    value = finding[name]
    if not isinstance(value, str):
        raise BaselineError(f"findings[{index}].{name} must be a string")
    return value


# {
# 責務: [_with_status: baseline entryへ差分statusを付ける]
# 処理: [1: entryを複製する, 2: statusを追加する]
# 引数: [entry: 元entry, status: NEW/EXISTING/RESOLVED]
# 戻り値: [statusを持つ新しいentry辞書]
# }
def _with_status(entry: Mapping[str, Any], status: str) -> dict[str, Any]:
    result = dict(entry)
    result["status"] = status
    return result
