# Checker Regression Matrix

UPD Commander Checker の4言語実装は、同じルールを同じ意味で回帰検証します。

## 共通必須ケース

| Case | Rule | Expected |
|---|---|---|
| dependency-ui-data | UPD101 | error |
| dependency-cross-application | UPD102 | error |
| data-commander-direct | UPD103 | warning |
| commander-loop | UPD201 | warning |
| commander-calculation | UPD202 | warning |
| commander-direct-work | UPD203 | error |
| upd301-two-inputs | UPD301 | no finding |
| upd301-boundary-default-2 | UPD301 | no finding |
| upd301-boundary-plus-one | UPD301 | attention |
| upd301-custom-max-1-two-inputs | UPD301 | attention |
| upd301-custom-max-3-three-inputs | UPD301 | no finding |
| upd301-custom-max-3-four-inputs | UPD301 | attention |
| multiple-outputs | UPD302 | attention |
| substantial-compresser-opportunity | UPD303 | warning |
| responsibility-too-large | UPD401 | warning |
| multiple-behavior-types | UPD402 | warning |
| colocated-data-type | UPD403 | attention |
| externally-used-colocated-data-type | UPD404 | warning |

## Ignore共通ケース

各言語で次を個別に確認します。

- CLI/path ignore: 対象ファイル全体が走査対象外になる
- `.updcommanderignore` rule + path: 指定ルールだけ抑止する
- inline `upd: ignore CODE`: 指定行の指定ルールだけ抑止する

## AST/構文解析の回帰ケース

ASTまたは同等の構文解析を使う言語は、次を共通で確認します。

- コメント中の疑似コードをルール違反として扱わない
- 文字列中の疑似コードをルール違反として扱わない
- 複数行宣言を正しく解析する
- nested type / nested block を外側の責務単位へ誤加算しない

## 言語別テスト入口

- Python: `python -m unittest discover -s tests`
- Go: `go test ./...`
- C++: `ctest --test-dir build --output-on-failure`
- C#: `dotnet test ../upd_commander_checker_tests/UpdCommanderChecker.Tests.csproj`

新しいルールを追加する場合は、この表へケースを追加し、4言語すべてで同じ期待値を固定します。言語仕様上完全に同型の入力を作れない場合でも、ルールの意味とseverityは一致させます。
