# Python UPD Commander Checker

Python プロジェクト向けの UPD Commander 静的文法・設計チェッカーです。

## 配置

```text
support_tools/python/upd_commander_checker/
```

他言語版は `support_tools/<language>/` に並列配置できます。

## インストール

```bash
python -m pip install -e support_tools/python/upd_commander_checker
```

## 実行

```bash
upd-commander-check path/to/project
```

または引数なしで `config/path.json` の `input` を使用できます。

```bash
upd-commander-check
```

出力は短くします。

```text
E UPD101 ui/view.py:3 UI layer must not depend directly on Data layer
E UPD102 applications/main/ui/view.py:4 direct dependency on another Application internal module
W UPD202 process/game_commander.py:12 Commander contains a calculation expression
FAIL e=2 w=1
```

問題なし:

```text
OK
```

## config/path.json

EXEビルド時に `dist/upd-commander-check/config/path.json` を自動生成します。CUIとGUIで同じ設定を使用し、既存ファイルは上書きしません。

```json
{
  "input": ".",
  "output": "",
  "ignore": ["tests/**", "generated/**"],
  "warnings_as_errors": false,
  "enabled_rules": ["UPD101", "UPD102", "UPD203"]
}
```

`input` と `output` の相対パスは `config` の親ディレクトリ基準です。`output` を設定すると、短い標準出力と同じ内容をファイルにも保存します。CLIの位置引数、`--output`、`--ignore`、`--warnings-as-errors` で設定を上書きできます。

`enabled_rules` は4言語で共通です。既存設定との互換性のため、項目自体がない場合は全ルールを有効にします。`[]` を明示するとルール検出をすべて停止しますが、対象不在や設定不正などの実行エラーは引き続き報告します。

## Nested Application

`apps/`、`applications/`、`features/` 等の直下名を Application 境界として認識します。

```text
applications/
├─ main/
│  ├─ ui/
│  ├─ process/
│  └─ data/
└─ settings/
   ├─ ui/
   ├─ process/
   └─ data/
```

各 Application は独立した UI / Process / Data を持つものとして判定します。

別 Application の内部層や Processing への直接 import は `UPD102` として検出します。Messenger および `contract` / `contracts` / `dto` / `dtos` / `shared` を含む共有契約 import は境界越し通信として許可します。

## Ignore

CLIでパス除外:

```bash
upd-commander-check . --ignore "tests/**" --ignore "generated/**"
```

`config/path.json` の `ignore` と、プロジェクト直下の `.updcommanderignore` も併用できます。

```text
generated/**
UPD202 process/fast_commander.py # performance hot path
```

性能上やむを得ない局所例外は、対象行へ明示できます。

```python
value = left + right  # upd: ignore UPD202 - performance hot path
```

`all` も使用できますが、原則として規則コードを指定してください。

警告も失敗扱い:

```bash
upd-commander-check . --warnings-as-errors
```

## GUI

`upd-commander-check-gui.exe` では、チェック対象ディレクトリ、結果出力先、Warningの失敗扱い、有効にするUPD番号を画面から設定できます。「設定を保存」でCUIと共通の `config/path.json` へ保存し、「チェック実行」で同じフォルダのCUI版を実行して結果を表示します。

## EXE ビルド

Windows:

```bat
build_exe.bat
```

または:

```bash
python -m pip install -e ".[build]"
python scripts/build_exe.py
```

生成先:

```text
dist/upd-commander-check/upd-commander-check.exe
dist/upd-commander-check/upd-commander-check-gui.exe
dist/upd-commander-check/config/path.json
```

## 現在のチェック

- `UPD001`: Python ソース読み込み失敗
- `UPD002`: Python 構文エラー
- `UPD101`: UPD Commander の層・役割依存規則違反
- `UPD102`: 別 Application 内部実装への直接依存
- `UPD103`: Data Commander同士の直接通信
- `UPD201`: Commander 内のループ
- `UPD202`: Commander 内の計算式
- `UPD203`: Commander 内の直接的な実処理/API 呼び出し
- `UPD301`: 複数入力によるContainer化候補
- `UPD302`: 複数返却値によるContainer化候補
- `UPD303`: Container/Compresserによる大幅圧縮候補
- `UPD401`: 責務単位が過大
- `UPD402`: 1ファイルに複数の主要責務型
- `UPD403`: データ型の同一ファイル配置
- `UPD404`: 外部利用されるデータ型の同一ファイル配置

静的解析だけでは断定しづらい項目は warning とし、確定的な違反と分離します。

## 判定方法

ファイルパス、ファイル名、import 名に含まれる `ui` / `process` / `data` と `commander` / `messenger` / `processing` を利用して役割を推定します。

Application 境界は `app` / `apps` / `application` / `applications` / `feature` / `features` の直下名から推定します。

UPD Commander はクラス必須ではないため、クラス構造自体は判定条件にしていません。
