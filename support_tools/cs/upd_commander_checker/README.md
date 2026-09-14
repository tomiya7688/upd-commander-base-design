# C# UPD Commander Checker

C#プロジェクト向けのUPD Commander静的チェッカーです。

.NET 8 + Roslyn (`Microsoft.CodeAnalysis.CSharp`) の構文木を使用し、UPD Commanderの依存規則・Application境界・Commander責務・Container/Compresser候補を解析します。コメントや文字列をC#構文として誤認せず、複数行宣言もSyntax Node単位で扱います。

## 実行

```bash
dotnet run --project UpdCommanderChecker.csproj -- path/to/project
```

引数なしなら `config/path.json` の `input` を使用します。

```bash
dotnet run --project UpdCommanderChecker.csproj
```

出力:

```text
E UPD102 applications/main/process/MainCommander.cs:3 cross-application internal dependency
W UPD202 process/GameCommander.cs:12 Commander calculation
FAIL e=1 w=1 a=0
```

問題なし:

```text
OK
```

## AST解析

ソースコードに対するルール判定はRoslyn SyntaxTreeを基準にします。

- `using` -> `UsingDirectiveSyntax`
- loop -> `ForStatementSyntax` / `ForEachStatementSyntax` / `WhileStatementSyntax` / `DoStatementSyntax`
- calculation -> `BinaryExpressionSyntax`
- I/O/API呼び出し -> `InvocationExpressionSyntax` / `ObjectCreationExpressionSyntax`
- 入出力Container候補 -> `BaseMethodDeclarationSyntax` / `TupleTypeSyntax`
- 責務単位 -> `TypeDeclarationSyntax`

パス分類、glob、Ignoreのように言語構文ではない処理にはASTを使用しません。

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

`input` / `output` の相対パスは `config` の親基準です。`output` を設定するとコンソールと同じ短い結果をファイルにも保存します。CLI指定は設定より優先されます。

`enabled_rules` は4言語で共通です。既存設定との互換性のため、項目自体がない場合は全ルールを有効にします。`[]` を明示するとルール検出をすべて停止しますが、対象不在や設定不正などの実行エラーは引き続き報告します。

## Ignore

```bash
dotnet run --project UpdCommanderChecker.csproj -- --ignore "tests/**" --ignore "generated/**" .
```

`config/path.json` の `ignore` と `.updcommanderignore` を併用できます。

```text
generated/**
UPD202 process/FastCommander.cs # performance hot path
```

行単位:

```csharp
var value = left + right; // upd: ignore UPD202 - performance hot path
```

## Application境界

`app/`, `apps/`, `application/`, `applications/`, `feature/`, `features/` の直下をApplication IDとして扱います。

別Applicationの内部実装への直接 `using` は `UPD102` です。Messenger、contract/contracts、dto/dtos、shared は境界APIとして許可します。

## EXEビルド

Windows:

```bat
build_exe.bat
```

内部では次を実行します。

```bash
dotnet publish UpdCommanderChecker.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
```

生成物:

```text
dist/upd-commander-check.exe
dist/config/path.json
```

self-contained のため.NETランタイム未導入環境でも実行できます。

成果物には `THIRD_PARTY_NOTICES.md` と `licenses/ROSLYN-LICENSE.txt` を同梱します。詳細は[リポジトリの第三者ライセンス一覧](../../../THIRD_PARTY_NOTICES.md)を参照してください。

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

C#コンパイラそのものの型検査等は `dotnet build` に任せ、このツールはRoslyn ASTを利用してUPD Commander規約を解析します。
