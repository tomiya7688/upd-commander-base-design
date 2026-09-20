# Support Tool Path Configuration

各言語の UPD Commander checker は、引数なし実行でも同じ設定方式を利用できるよう `config/path.json` を共通設定として使用します。

## 形式

```json
{
  "input": ".",
  "output": "",
  "ignore": [
    "tests/**",
    "generated/**"
  ],
  "warnings_as_errors": false,
  "common_roots": ["common", "shared"],
  "enabled_rules": ["UPD101", "UPD102", "UPD203"]
}
```

## 項目

- `input`: 検査対象ファイルまたはディレクトリ。相対パスは `config` ディレクトリの親を基準に解決する。
- `output`: 検査結果を書き出すテキストファイル。空文字列の場合はファイル出力しない。
- `ignore`: 除外するパスglobの配列。既存の `.updcommanderignore` および行単位Ignoreと併用できる。
- `warnings_as_errors`: `true` の場合、warning が1件以上あれば終了コードを失敗にする。
- `common_roots`: 層中立 Common / Shared として認識するディレクトリ名。既定は `["common", "shared"]`。空配列で自動認識を無効化する。各値は空でない単一ディレクトリ名で、path separator・`.`・`..`・UI/Process/Dataのlayer名は不可。
- `enabled_rules`: 有効にするUPD番号の配列。省略時は全ルール、空配列は全ルール無効。未知の番号は設定エラー。

## 探索順

1. 実行ファイルの隣にある `config/path.json`
2. カレントディレクトリの `config/path.json`
3. 見つからない場合は既定値を使用

## 優先順位

`path.json` は通常実行の既定値です。CLIで明示された値は設定を上書きします。

- 位置引数 -> `input` を上書き
- `--output` -> `output` を上書き
- `--ignore` -> `ignore` に追加
- `--warnings-as-errors` -> `warnings_as_errors=true` として扱う

`.updcommanderignore` と行単位Ignoreは引き続き有効です。

## ビルド時生成

各言語の単体実行ファイルをビルドすると、実行ファイル側に `config/path.json` が存在しない場合のみ既定設定を自動生成します。

既存の `path.json` は上書きしません。これにより一度設定したプロジェクトパスやIgnore設定を再ビルドで失わないようにします。

## 出力

`output` が設定されている場合でも、短い標準出力は維持します。同じ内容を指定ファイルにも保存します。
