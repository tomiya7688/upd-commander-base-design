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

## AST解析

ソースコードに対する規則判定はlibclangのTranslation Unit / AST cursorを基準にします。

- `#include` -> `CXCursor_InclusionDirective`
- class -> `CXCursor_ClassDecl` / `CXCursor_ClassTemplate`
- function/method -> `CXCursor_FunctionDecl` / `CXCursor_CXXMethod` / `CXCursor_Constructor`
- loop -> `CXCursor_ForStmt` / `CXCursor_CXXForRangeStmt` / `CXCursor_WhileStmt` / `CXCursor_DoStmt`
- calculation -> `CXCursor_BinaryOperator`
- I/O/API呼び出し -> `CXCursor_CallExpr` / `CXCursor_VarDecl`

コメントや文字列内の疑似C++コードはASTノードにならないため、規則判定対象になりません。パス分類、glob、IgnoreのようにC++構文ではない処理にはASTを使用しません。

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
  "warnings_as_errors": false
}
```

`input` / `output` の相対パスは `config` の親基準です。`output` を指定するとコンソールと同じ短い結果をファイルへ保存します。CLIの位置引数、`--output`、`--ignore`、`--warnings-as-errors` は設定を上書き・追加します。

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
- `UPD002`: AST解析失敗
- `UPD101`: UI / Process / Data・Commander / Messenger / Processing依存違反
- `UPD102`: Application境界越しの内部実装直接依存
- `UPD103`: Data Commander同士の直接通信
- `UPD201`: Commander内のループ
- `UPD202`: Commander内の計算式
- `UPD203`: Commander内の直接I/O/API呼び出し
- `UPD301`: 複数入力によるContainer化候補 (`attention`)
- `UPD302`: 複数返却値によるContainer化候補 (`attention`)
- `UPD303`: Container/Compresser導入でCommander/Messengerの大幅圧縮が見込まれる場合 (`warning`)
- `UPD401`: 責務単位が過大
- `UPD402`: 1ファイルに複数の主要責務クラス

## テスト

```bash
cmake -S . -B build
cmake --build build
ctest --test-dir build --output-on-failure
```

C++コンパイラそのものの型検査等は通常のビルドへ任せ、このツールはClang ASTを利用してUPD Commander規約を解析します。
