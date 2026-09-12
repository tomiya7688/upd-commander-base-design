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
  "warnings_as_errors": false
}
```

`input` / `output` の相対パスは `config` の親基準です。`output` を指定するとコンソール出力と同じ結果をファイルにも保存します。CLI指定は設定より優先されます。

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

- `UPD002`: Go構文エラー
- `UPD101`: UI / Process / Data・Commander / Messenger / Processing依存違反
- `UPD102`: Application境界越しの内部実装直接依存
- `UPD201`: Commander内のループ
- `UPD202`: Commander内の計算式
- `UPD203`: Commander内の直接I/O/API呼び出し

## テスト

```bash
go test ./...
```
