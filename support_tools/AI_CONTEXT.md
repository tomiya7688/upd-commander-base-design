# Checker Development AI Context

> Scope: `support_tools/` の UPD Commander Checker 開発。root `AI_CONTEXT.md` の共通ルールを前提とし、ここには Checker 固有の差分だけを書く。

## Start

1. current Issue / PR を読む。
2. `main` との差分を確認する。
3. 変更を `language-local` / `shared-rule` / `config-output` / `build-CI` に分類する。
4. 下の routing から必要な source / test / spec だけ読む。
5. Acceptance を満たす情報が揃ったら探索を止める。

## Language Roots

```text
Python  support_tools/python/upd_commander_checker/
Go      support_tools/go/upd_commander_checker/
C++     support_tools/cpp/upd_commander_checker/
C#      support_tools/cs/upd_commander_checker/
```

各言語実装は独立パッケージ。別言語Checkerをコード依存させない。

## Rule Routing

### UPD101 / UPD102 / UPD103 — Dependency
Read:
- 対象言語の `dependency*` analyzer/rules
- classifier / module-role 判定は必要時のみ
- `specification/dependency-rules.md`
- `specification/application-boundary.md` (`UPD102`)
- `specification/data-commander-communication.md` (`UPD103`)

Tests:
- dependency / application boundary / Data Commander regression

4言語へ広げる条件:
- rule意味、code、severity、Application境界の共通定義を変更するとき

### UPD201 / UPD202 / UPD203 — Commander
Read:
- 対象言語の `commander*` analyzer
- `specification/commander-spec.md`

Tests:
- loop / calculation / direct-work regression
- AST言語では comment/string false-positive regression

4言語へ広げる条件:
- Commanderで許可/禁止する処理の意味を変えるとき

### UPD301 / UPD302 / UPD303 — Container / Compresser
Read:
- 対象言語の `container*` analyzer/rules
- `specification/compresser-spec.md`
- `specification/compresser-check-rules.md`

Tests:
- multiple input/output
- substantial compression threshold

4言語へ広げる条件:
- severity、圧縮判定、共通しきい値を変更するとき

### UPD401 / UPD402 / UPD403 / UPD404 — Responsibility / Type location
Read:
- 対象言語の `responsibility*` rules
- `data_type_location*` / 相当実装（403/404）
- `specification/responsibility-check-rules.md`

Tests:
- 250/251 lines
- 12/13 methods
- multiple behavioral types
- colocated data-only type
- external reference promotion to UPD404

4言語へ広げる条件:
- しきい値、data-only定義、severity、型配置ルールを変更するとき

### Config / Ignore / Output
Read:
- `config*` loader/model
- `ignore*` rules
- CLI / output formatter / finding emitter
- `support_tools/config-path.md`

Tests:
- invalid config
- CLI/path ignore
- rule ignore
- inline ignore
- short output compatibility

### AST / Parser infrastructure
Read:
- Scanner / AST analyzer / parser entry only
- target analyzer
- matching AST regression

Do not read every rule analyzer unless parser API changes require them.

## Responsibility Map

```text
Scanner                 file enumeration + parse setup + analyzer orchestration
Classifier              UI/Process/Data, Commander/Messenger/Processing, Application classification
Dependency analyzer     UPD101-103
Commander analyzer      UPD201-203
Container analyzer      UPD301-303
Responsibility rules    UPD401-402
Data type location      UPD403-404
Ignore rules            path/rule/inline suppression
Finding emitter/output  stable finding + short CLI output
Config loader/model     config/path.json + defaults + explicit config errors
Tests                    rule contract / regression / false-positive protection
```

Scannerへ新rule条件を直接追加しない。新ruleは対応analyzer/rulesへ置く。

## Validation Routing

### Python only
```text
cd support_tools/python/upd_commander_checker
python -m unittest discover -s tests
```
その後 Python strict self-check。正確なCI手順は `.github/workflows/checker-self-check.yml` をSource of Truthとする。

### Go only
```text
cd support_tools/go/upd_commander_checker
go test ./...
```
その後 Go strict self-check。

### C++ only
build/config変更を伴わないrule変更でも、AST/libclangを使うため build + `ctest` を実施する。
```text
ctest --test-dir build --output-on-failure
```
正確なconfigure/build依存は workflow を参照する。

### C# only
```text
cd support_tools/cs/upd_commander_checker
dotnet test ../upd_commander_checker_tests/UpdCommanderChecker.Tests.csproj -c Release
```
その後 C# strict self-check。

### Shared rule/spec change
4言語のmatching regressionを更新し、`Checker Self Check` 全jobを確認する。責務境界変更なら `CSharp Responsibility Thresholds` も確認する。

## Exploration Stop

次が揃ったら broad search を止める。

- failing / requested ruleが特定済み
- target languageが特定済み
- source ownerが特定済み
- matching testが特定済み
- relevant specificationが特定済み
- Acceptanceが明確

実装中に新しい依存や矛盾が見つかった場合だけ探索を再開する。

## Remote Delta First

このrepoは複数チャットから短時間にPRがマージされる。新規branch作成前とPR conflict解消前は必ず最新 `main` を確認する。

優先順:
```text
latest main commit
→ changed filenames / compact diff
→ current Issue / PR
→ target source
```

古い会話で覚えているファイル構造をremoteより優先しない。

## Context Output Policy

AIへの報告は基本的に以下だけでよい。

```text
changed: <files / responsibility>
validation: <passed / failed step>
unverified: <only if any>
PR: <number>
```

成功ログ全文、全tree、無関係な仕様要約は出さない。
