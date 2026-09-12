# Python UPD Commander Checker

Python プロジェクト向けの UPD Commander 静的文法・設計チェッカーです。

## 配置

このツールは言語別 support tool として独立パッケージ化しています。

```text
support_tools/python/upd_commander_checker/
```

今後 Go / C++ / C# 版などを `support_tools/<language>/` に並列追加できます。

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

除外指定:

```bash
upd-commander-check . --ignore "tests/**" --ignore "generated/**"
```

警告も失敗扱いにする場合:

```bash
upd-commander-check . --warnings-as-errors
```

## 現在のチェック

- `UPD001`: Python ソース読み込み失敗
- `UPD002`: Python 構文エラー
- `UPD101`: UPD Commander の依存規則違反
- `UPD201`: Commander 内のループ（要確認）
- `UPD202`: Commander 内の計算式（要確認）
- `UPD203`: Commander 内の直接的な実処理/API 呼び出し

静的解析だけでは確定できない規約は warning とし、機械的に確定できる違反と分離します。

## 判定方法

ファイルパス、ファイル名、import 名に含まれる `ui` / `process` / `data` と `commander` / `messenger` / `processing` を利用して役割を推定します。

UPD Commander はクラス必須ではないため、クラス構造そのものは判定条件にしていません。
