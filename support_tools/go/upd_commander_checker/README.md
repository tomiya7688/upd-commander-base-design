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

## Finding baseline

初回スキャン結果を対象ルートの `.upd-baseline.json` に保存します。保存後も通常のFinding出力と終了コードは変わりません。

```bash
go run ./cmd/upd-commander-check --write-baseline path/to/project
```

保存先を指定する場合は `--write-baseline=PATH` を使います。既存baselineと比較すると、通常のFindingに `NEW` / `EXISTING` が付き、解消済みFindingも `RESOLVED` として表示されます。

```bash
go run ./cmd/upd-commander-check --write-baseline=.upd-baseline.json path/to/project
go run ./cmd/upd-commander-check --baseline=.upd-baseline.json path/to/project
```

破損したbaselineや未対応schema/fingerprint versionは `BASELINE ERROR` として終了コード2で報告します。`--write-baseline` と `--baseline` は同時に指定できません。

## config/path.json

`build_exe.bat` 実行時に `dist/config/path.json` を自動生成します。既存ファイルは上書きしません。

```json
{
  "input": ".",
  "output": "",
  "ignore": ["tests/**", "generated/**"],
  "warnings_as_errors": false,
  "fail_on": ["error", "warning"],
  "severity_overrides": {"UPD203": "warning"},
  "gate_exceptions": [
    {"rule": "UPD203", "path": "applications/main/process/main.go", "line": 42, "reason": "approved I/O boundary"}
  ],
  "enabled_rules": ["UPD101", "UPD102", "UPD203"]
}
```

`input` / `output` の相対パスは `config` の親基準です。`output` を指定するとコンソール出力と同じ結果をファイルにも保存します。CLI指定は設定より優先されます。

`gate_exceptions` はrule・相対path・任意の1始まりlineでgate対象を限定し、必須の理由をFinding行へ表示します。対象Findingと集計は残りますが、CI gateだけを通過できます。pathはscan targetからの大文字小文字を区別する `/` 区切り完全一致です。`ignore` のように検出・出力を抑止しません。

`enabled_rules` は4言語で共通です。既存設定との互換性のため、項目自体がない場合は全ルールを有効にします。`[]` を明示するとルール検出をすべて停止しますが、対象不在や設定不正などの実行エラーは引き続き報告します。

`fail_on` は `error` / `warning` / `attention` のうち、終了コード1にするseverityを指定します。未指定時は従来どおりerrorが失敗し、`warnings_as_errors` と `--attentions-as-errors` で追加できます。明示的な `fail_on` または `--fail-on` は従来フラグより優先されます。`"fail_on": []` または `--fail-on ""` はfindingによる失敗を無効にします。CLI例: `--fail-on error,warning --severity-override UPD203=warning`。`severity_overrides` / `--severity-override` はfindingを保持したまま有効severityを変更し、表示・集計・ゲートに反映します。CLI overrideは同じルールの設定値より優先です。

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
