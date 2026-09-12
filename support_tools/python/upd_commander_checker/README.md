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

または:

```bash
python -m upd_commander_checker path/to/project
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

プロジェクト直下の `.updcommanderignore` でも設定できます。

```text
# 全チェックを除外
generated/**

# 特定規則だけ除外。理由はコメントに残す
UPD202 process/fast_commander.py # performance hot path
```

性能上やむを得ない局所例外は、対象行へ明示できます。

```python
value = left + right  # upd: ignore UPD202 - performance hot path
```

`all` も使用できますが、原則として規則コードを指定してください。

```python
fast_call()  # upd: ignore all - generated bridge
```

警告も失敗扱い:

```bash
upd-commander-check . --warnings-as-errors
```

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
dist/upd-commander-check.exe
```

## 現在のチェック

- `UPD001`: Python ソース読み込み失敗
- `UPD002`: Python 構文エラー
- `UPD101`: UPD Commander の層・役割依存規則違反
- `UPD102`: 別 Application 内部実装への直接依存
- `UPD201`: Commander 内のループ
- `UPD202`: Commander 内の計算式
- `UPD203`: Commander 内の直接的な実処理/API 呼び出し

静的解析だけでは断定しづらい項目は warning とし、確定的な違反と分離します。

## 判定方法

ファイルパス、ファイル名、import 名に含まれる `ui` / `process` / `data` と `commander` / `messenger` / `processing` を利用して役割を推定します。

Application 境界は `app` / `apps` / `application` / `applications` / `feature` / `features` の直下名から推定します。

UPD Commander はクラス必須ではないため、クラス構造自体は判定条件にしていません。
