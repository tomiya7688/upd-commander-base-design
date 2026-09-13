# AI Context

UPD Commander Base Design を AI が必要以上に読み広げず、安全に変更するための入口です。

## Core invariants

- UI / Process / Data の責務境界を維持する
- Commander は routing / dispatch を担当し、実処理を持たない
- Messenger は層間・Application間通信を担当し、実処理を持たない
- Processing が実処理を担当する
- UI -> Data の直接依存や別層 Processing の直接呼び出しを追加しない
- `docs/` は説明、`specification/` は規定の source of truth として扱う

## Read routing

現在タスクに必要な文書だけを先に読みます。

| Task | First read | Read next only if needed |
|---|---|---|
| 全体構造 / 責務 | `README.md`, `docs/architecture.md` | `specification/layer-spec.md` |
| Layer責務 | `docs/layer-rules.md` | `specification/layer-spec.md`, `specification/dependency-rules.md` |
| Commander変更 | `docs/commander-messenger.md` | `specification/commander-spec.md`, `specification/dependency-rules.md` |
| Messenger / 通信 | `docs/commander-messenger.md`, `docs/data-flow.md` | `specification/messenger-spec.md`, `specification/message-contract.md` |
| Processing | `docs/architecture.md` | `specification/processing-spec.md` |
| Application境界 | `docs/architecture.md` | `specification/application-boundary.md` |
| Error handling | relevant flow doc | `specification/error-handling.md` |
| Testing / validation | relevant responsibility spec | `specification/testing-rules.md`, `specification/implementation-quality.md` |
| 規約変更 | `specification/README.md` | affected specification file only |
| Anti-pattern判断 | `docs/anti-patterns.md` | corresponding specification file |

## Change routing

変更対象から最初の working set を限定します。

```text
UI concern
  -> UI Commander / UI Processing / UI Messenger

Process concern
  -> Process Commander / Process Processing / Process Messenger

Data concern
  -> Data Commander / Data Processing / Data Messenger

cross-layer contract
  -> sender Messenger / message contract / receiver Messenger

cross-Application concern
  -> Application boundary / contract / explicit routing point
```

別layerや別Applicationは、contract変更・dependency変更・validation failureが示した場合だけ追加で読みます。

## Exploration stop

次が揃ったら追加探索を止めます。

- Goal
- affected layer / Application
- required contract / rule
- target files or documents
- Acceptance / validation route

不明点が残る場合は全体を読むのではなく、その不明点を解決する最小の specification だけ追加取得します。

## Source of truth

- normative rule: `specification/`
- explanatory design: `docs/`
- concise project entry: `README.md`
- helper/checker output: evidence / index only

説明と規定が衝突する場合は `specification/` を優先します。

## Validation

- 文書変更: affected docs/spec の整合性とリンクを確認
- 規定変更: 対応する explanatory doc と checker / examples への影響を確認
- checker変更: targeted checker validationを先に実行
- unrelated formatting / refactorを同時に広げない

完了報告は changed files / rule impact / validation / unverified を短くまとめます。
