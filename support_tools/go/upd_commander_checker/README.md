# Go UPD Commander Checker

Goプロジェクト向けのUPD Commander静的チェッカーです。

## 実行

```bash
go run ./cmd/upd-commander-check path/to/project
```

引数なしなら `config/path.json` の `input` を使用します。

```bash
go run ./cmd/upd-commander-check
```

出力は短くします。

```text
E UPD102 applications/main/process/main_commander.go:3 cross-application internal dependency
W UPD202 process/game_commander.go:12 Commander calculation
FAIL e=1 w=1
```

問題なし:

```text
OK
```

## config/path.json

`build_exe.bat` 実行時に `dist/config/path.json` を自動生成します。既存ファイルは上書きしません。

```json
{
  "input": ".",
  "output": "",
  "ignore": ["tests/**", "generated/**"],
  "warnings_as_errors": false,
  "enabled_rules": ["UPD101", "UPD102", "UPD203"]
}
```

`input` / `output` の相対パスは `config` の親基準です。`output` を指定するとコンソール出力と同じ結果をファイルにも保存します。CLI指定は設定より優先されます。

`enabled_rules` は4言語で共通です。既存設定との互換性のため、項目自体がない場合は全ルールを有効にします。`[]` を明示するとルール検出をすべて停止しますが、対象不在や設定不正などの実行エラーは引き続き報告します。

## Ignore

CLI:

```bash
go run ./cmd/upd-commander-check --ignore "tests/**" --ignore "generated/**" .
```

`config/path.json` の `ignore` と `.updcommanderignore` を併用できます。

```text
generated/**
UPD202 process/fast_commander.go # performance hot path
```

行単位:

```go
value := left + right // upd: ignore UPD202 - performance hot path
```

## Application境界

以下のような構成では `main` と `settings` を別Applicationとして扱います。

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

別Applicationの内部実装への直接importは `UPD102` です。Messenger、contract/contracts、dto/dtos、shared はApplication間境界として許可します。

## EXEビルド

Windows:

```bat
build_exe.bat
```

生成物:

```text
dist/upd-commander-check.exe
dist/config/path.json
```

Go標準ライブラリのみで実装しているため、PyInstaller等は不要です。

## 規則

- `UPD001`: ソース読み込み失敗
- `UPD002`: 構文・AST解析エラー
- `UPD101`: UI / Process / Data・Commander / Messenger / Processing依存違反
- `UPD102`: Application境界越しの内部実装直接依存
- `UPD103`: Data Commander同士の直接通信
- `UPD201`: Commander内のループ
- `UPD202`: Commander内の計算式
- `UPD203`: Commander内の直接I/O/API呼び出し
- `UPD301`: 複数入力によるContainer化候補 (`attention`)
- `UPD302`: 複数返却値によるContainer化候補 (`attention`)
- `UPD303`: Container/Compresser導入による大幅圧縮候補 (`warning`)
- `UPD401`: 責務単位が過大
- `UPD402`: 1ファイルに複数の主要責務型
- `UPD403`: データ型の同一ファイル配置 (`attention`)
- `UPD404`: 外部利用されるデータ型の同一ファイル配置 (`warning`)

## テスト

```bash
go test ./...
```
