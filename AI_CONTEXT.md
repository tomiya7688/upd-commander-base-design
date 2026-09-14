# AI Context

> UPD Commander Base Design / Checker 開発で AI が最初に読む小さい索引です。詳細仕様はここへ複製せず、必要な原典へ routing します。

## Project
- Purpose: UI / Process / Data と Commander / Messenger / Processing の責務境界を定義し、4言語 Checker で静的検証する。
- Checker languages: Python / Go / C++ / C#。
- Development context policy: `ai-context-reducer` の Search-first / Exploration Stop / targeted validation を採用する。

## Source of Truth
- 設計概要: `README.md`
- 技術仕様: `specification/README.md`
- Checker共通回帰契約: `specification/checker-regression-matrix.md`
- Checker実装: `support_tools/<language>/upd_commander_checker/`
- Checker開発routing: `support_tools/AI_CONTEXT.md`
- CI: `.github/workflows/checker-self-check.yml`
- Task: GitHub Issue / PR の本文と最新コメント

## Read First
Checker変更では原則として次の順だけ読む。

1. current Issue / PR
2. `support_tools/AI_CONTEXT.md`
3. 対象ruleの specification 1〜2文書
4. 対象言語の source + matching tests
5. direct dependency only when needed

README、全specification、4言語sourceを最初から全部読まない。

## Current Task Rules
- Search first, read second。
- Goal / Required / Acceptance / target language が揃ったら探索を止める。
- 1言語固有bugは対象言語から始め、共通契約変更でない限り他3言語を先読みしない。
- rule意味・severity・共通しきい値を変更する場合だけ4言語同期へ広げる。
- unrelated refactor を混ぜない。
- 要約より source / tests / specification の原典を優先する。

## Ignore Normally
- `bin/`, `obj/`, `build/`, cache
- generated artifacts / packaged executables
- 成功したCIの全文ログ
- unrelated Issues / PR history
- `licenses/` と third-party 文書（ライセンス変更時を除く）
- 英語版文書（翻訳変更時を除き、日本語仕様を正本として読む）

## Important Constraints
- Checker自身も UPD strict self-check に適合する。
- Checker出力は短く、rule code / severity / path / line を安定させる。
- Ignore と既存rule compatibilityを壊さない。
- `ai-context-reducer` は開発支援であり Checker の runtime/build dependency にしない。
- 4言語の同一ruleは意味とseverityを一致させる。

## Validation
- 1言語固有変更: matching unit/regression tests → その言語の strict self-check。
- 共通rule変更: 4言語 matching tests → `Checker Self Check` 全言語。
- C# responsibility境界変更: `CSharp Responsibility Thresholds` も確認。
- docs/routing only: link/path整合を確認し、不要なfull buildはしない。
- 失敗時だけ必要範囲のlogを読む。成功log全文はcontextへ入れない。

## Remote Delta
複数チャット/AIから更新される前提なので、実装前に `main` と作業branchの changed files / commit summary を確認する。古い会話内容より remote の最新状態を優先する。

## Context Priority
- P0: current Issue / Acceptance / user decision
- P1: target source / matching tests
- P2: direct rule specification / direct dependencies
- P3: checker routing / architecture references
- P4: history / unrelated docs / broad repository context
