# Finding Baseline Contract

**日本語** | [English](baseline-contract.en.md)

この文書はFinding identityとbaselineファイルのversion 1契約を定義する。4言語Checkerは同じ入力から同じfingerprintを生成する。

## 1. Finding identity

fingerprintの入力は次の4要素である。

| Field | Required | Canonical form |
|---|---:|---|
| `rule` | Yes | `UPD` + 3桁以上の数字、大文字 |
| `path` | Yes | repository root相対、`/`区切り、`.`要素除去、`..`と絶対pathは禁止 |
| `symbol` | Yes | qualified type/function/member等。特定できない場合は空文字 |
| `context` | Yes | rule固有の安定した識別文脈。空文字は禁止 |

文字列はUnicode NFCへ正規化する。pathの大文字・小文字は保持する。symbol/contextには表示用文章や絶対pathを使わず、同一ファイル内の同一ruleに複数findingがある場合は別findingを識別できる安定情報を含める。例として依存findingのcontextには正規化済みtarget identityを含める。

次の値はfingerprintに含めない。

- 行番号、列番号、周辺source行
- severity
- 表示用message
- 時刻、絶対path、実行環境固有値

従って行挿入やseverity変更、説明文修正だけではidentityが変わらない。rule/path/symbol/contextのいずれかが変われば別identityとなる。contextは位置番号の代用品ではなく、ruleの意味に基づく安定した識別情報とする。

## 2. Fingerprint algorithm v1

1. 上表の正規化を適用する。
2. 次の5つのUTF-8文字列を、この順でU+0000 byteで連結する。各field内のU+0000は禁止する。

   `upd-finding-fingerprint-v1`, `rule`, `path`, `symbol`, `context`

3. 連結したbyte列のSHA-256を計算し、小文字hexで `sha256:<hex>` と表す。

固定domain separatorとfield順はversion 1の一部である。正規化やfield構成を互換性なく変える場合はfingerprint versionを増やす。

同じfingerprintが1回のscan内で複数回生成された場合、Checkerはそれらを黙って1件に畳み込んではならない。contextの不足として診断し、identityを一意にできるようにする。

## 3. Baseline JSON schema v1

```json
{
  "schema_version": 1,
  "fingerprint_version": 1,
  "findings": [
    {
      "fingerprint": "sha256:<64 lowercase hex characters>",
      "rule": "UPD101",
      "path": "src/ui/screen.cs",
      "symbol": "Ui.Screen.Run",
      "context": "target=data.storage",
      "severity": "error",
      "line": 24,
      "message": "UI must not depend on Data"
    }
  ]
}
```

`schema_version`, `fingerprint_version`, `findings`,および各entryの`fingerprint`, `rule`, `path`, `symbol`, `context`, `severity`は必須である。`line`と`message`は表示・追跡用の任意metadataでありidentityには影響しない。entryのfingerprintは正規化済みidentityから再計算した値と一致しなければならない。baseline内のfingerprintは一意でなければならない。

## 4. Compatibility and errors

- 同一schema versionで未知の追加propertyは読み飛ばす。
- 未対応の新しいschema/fingerprint versionは推測して読み込まず、対応versionと実ファイルversionを示す明確なエラーにする。
- 古いversionは明示的なmigrationが実装されている場合のみ読み込む。それ以外は明確なunsupported-version errorにする。
- 必須field欠落、型不一致、不正fingerprint、重複fingerprint、不正pathはbaseline破損として扱う。
- 空の`findings`は有効なbaselineである。

## 5. Shared fixtures

[baseline-fingerprint-fixtures.json](baseline-fingerprint-fixtures.json) がgolden vectorを定義する。fixture testは行番号・severity・正規化表記の変更に対する安定性と、rule/path/symbol/contextそれぞれの差がfingerprintを分離することを検証する。
