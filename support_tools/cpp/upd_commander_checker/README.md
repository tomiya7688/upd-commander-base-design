# C++ UPD Commander Checker

C++プロジェクト向けのUPD Commander静的チェッカーです。

C++17でチェッカー本体を実装し、ソース解析には Clang/libclang のASTを使用します。UPD Commanderの依存規則・Application境界・Commander責務・Container/Compresser候補を、正規表現によるC++構文推測ではなくTranslation UnitのASTから判定します。

## 必要環境

- CMake 3.20+
- C++17コンパイラ
- LLVM / libclang development files

Ubuntu例:

```bash
sudo apt-get install libclang-dev llvm-dev
```

WindowsではLLVMをインストールし、CMakeから `clang-c/Index.h` とlibclangライブラリを参照できる状態にしてください。一般的な `C:\Program Files\LLVM` 配置を推奨します。

## ビルド

Windows:

```bat
build_exe.bat
```

または:

```bash
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release --target upd-commander-check
```

libclangが見つからない場合、configure時に明示的にエラーにします。

Windows向け成果物には `THIRD_PARTY_NOTICES.md` と `licenses/LLVM-LICENSE.txt` を同梱します。詳細は[リポジトリの第三者ライセンス一覧](../../../THIRD_PARTY_NOTICES.md)を参照してください。

## AST解析

ソースコードに対する規則判定はlibclangのTranslation Unit / AST cursorを基準にします。

- `#include` -> `CXCursor_InclusionDirective`
- class -> `CXCursor_ClassDecl` / `CXCursor_ClassTemplate`
- function/method -> `CXCursor_FunctionDecl` / `CXCursor_CXXMethod` / `CXCursor_Constructor`
- loop -> `CXCursor_ForStmt` / `CXCursor_CXXForRangeStmt` / `CXCursor_WhileStmt` / `CXCursor_DoStmt`
- calculation -> `CXCursor_BinaryOperator`
- I/O/API呼び出し -> `CXCursor_CallExpr` / `CXCursor_VarDecl`

コメントや文字列内の疑似C++コードはASTノードにならないため、規則判定対象になりません。パス分類、glob、IgnoreのようにC++構文ではない処理にはASTを使用しません。

## compile_commands.json

実プロジェクトと同じinclude path、macro、C++標準、platform define等でASTを構築するため、C++ checkerは `compile_commands.json` を自動利用します。

探索順:

1. 解析対象ファイルの親ディレクトリ
2. その親階層をルート方向へ順次探索
3. 最初に見つかった `compile_commands.json` を使用
4. 対象sourceのcompile commandがあればその引数を優先
5. header等で直接commandが無ければdatabase内の関連translation unitの引数を利用
6. databaseが無い、または利用可能なcommandが無い場合は従来の軽量fallbackを使用

fallbackは次の内容です。

```text
-x c++
-std=c++17
-I<scan root>
-I<target file parent>
```

Compilation Databaseから取得したcompiler executable、`-c`、source入力、`-o`等の出力専用引数はlibclang解析用には渡しません。compile commandのworking directoryは保持するため、相対 `-I` 等も実ビルドと同じ基準で解決されます。

CMakeプロジェクトなら一般的には次のように生成できます。

```bash
cmake -S . -B build -DCMAKE_EXPORT_COMPILE_COMMANDS=ON
```

`build/compile_commands.json` をプロジェクトルートへ配置またはリンクする構成でも利用できます。

## 実行

```bash
upd-commander-check path/to/project
```

引数なしなら実行ファイル側の `config/path.json` を使用します。

```bash
upd-commander-check
```

出力:

```text
E UPD102 applications/main/process/main_commander.cpp:3 cross-application internal dependency
W UPD202 process/game_commander.cpp:12 Commander calculation
FAIL e=1 w=1 a=0
```

問題なし:

```text
OK
```

## config/path.json

`build_exe.bat` 実行時にEXEと同じ出力ディレクトリの `config/path.json` を自動生成します。既存ファイルは上書きしません。

```json
{
  "input": ".",
  "output": "",
  "ignore": ["tests/**", "generated/**"],
  "warnings_as_errors": false,
  "enabled_rules": ["UPD101", "UPD102", "UPD203"]
}
```

`input` / `output` の相対パスは `config` の親基準です。`output` を指定するとコンソールと同じ短い結果をファイルへ保存します。CLIの位置引数、`--output`、`--ignore`、`--warnings-as-errors` は設定を上書き・追加します。

`enabled_rules` は4言語で共通です。既存設定との互換性のため、項目自体がない場合は全ルールを有効にします。`[]` を明示するとルール検出をすべて停止しますが、対象不在や設定不正などの実行エラーは引き続き報告します。

## Ignore

```bash
upd-commander-check --ignore "tests/**" --ignore "generated/**" .
```

`config/path.json` の `ignore` と `.updcommanderignore` を併用できます。

```text
generated/**
UPD202 process/fast_commander.cpp # performance hot path
```

行単位:

```cpp
value = left + right; // upd: ignore UPD202 - performance hot path
```

## Application境界

`app/`, `apps/`, `application/`, `applications/`, `feature/`, `features/` の直下をApplication IDとして扱います。

別Applicationの内部実装への直接 `#include` は `UPD102` です。Messenger、contract/contracts、dto/dtos、shared は境界APIとして許可します。

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
cmake -S . -B build
cmake --build build
ctest --test-dir build --output-on-failure
```

C++コンパイラそのものの型検査等は通常のビルドへ任せ、このツールはClang ASTを利用してUPD Commander規約を解析します。
