# C# UPD Commander Checker

C#プロジェクト向けのUPD Commander静的チェッカーです。

.NET 8標準ライブラリのみで実装し、UPD Commanderの依存規則・Application境界・Commander責務を軽量に検査します。

## 実行

```bash
dotnet run --project UpdCommanderChecker.csproj -- path/to/project
```

出力:

```text
E UPD102 applications/main/process/MainCommander.cs:3 cross-application internal dependency
W UPD202 process/GameCommander.cs:12 Commander calculation
FAIL e=1 w=1
```

問題なし:

```text
OK
```

## Ignore

```bash
dotnet run --project UpdCommanderChecker.csproj -- --ignore "tests/**" --ignore "generated/**" .
```

`.updcommanderignore`:

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

生成物は `dist/upd-commander-check.exe` です。self-contained のため.NETランタイム未導入環境でも実行できますが、ファイルサイズは大きくなります。

## 規則

- `UPD001`: ソース読み込み失敗
- `UPD101`: UI / Process / Data・Commander / Messenger / Processing依存違反
- `UPD102`: Application境界越しの内部実装直接依存
- `UPD201`: Commander内のループ
- `UPD202`: Commander内の計算式（軽量ヒューリスティック）
- `UPD203`: Commander内の直接I/O/API呼び出し（軽量ヒューリスティック）

完全なC#構文検査は通常の `dotnet build` に任せ、このツールはUPD Commander規約の静的検査に集中します。
