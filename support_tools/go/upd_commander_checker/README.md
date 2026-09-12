# Go UPD Commander Checker

Goプロジェクト向けのUPD Commander静的チェッカーです。

## 実行

```bash
go run ./cmd/upd-commander-check path/to/project
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

## Ignore

CLI:

```bash
go run ./cmd/upd-commander-check --ignore "tests/**" --ignore "generated/**" .
```

`.updcommanderignore`:

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

または:

```bash
go build -o dist/upd-commander-check.exe ./cmd/upd-commander-check
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
