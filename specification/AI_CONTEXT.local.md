# Specification AI Context

このディレクトリは規定の source of truth です。通常タスクで全文を読む必要はありません。

## Scope

現在変更する規定と、その直接依存だけを読みます。

- layer責務 -> `layer-spec.md`
- Commander責務 -> `commander-spec.md`
- Messenger責務 -> `messenger-spec.md`
- Processing責務 -> `processing-spec.md`
- dependency -> `dependency-rules.md`
- Application境界 -> `application-boundary.md`
- message contract -> `message-contract.md`
- error -> `error-handling.md`
- tests -> `testing-rules.md`
- quality gate -> `implementation-quality.md`
- 推奨事項 -> `recommended-practices.md`

## Rules

- `README.md` の最上位規定を破らない
- Required rule と Recommended practice を混同しない
- 例外は reason / scope / mitigation / removal condition を明示する
- 1つの規定変更を理由に無関係なspecまで読み広げない
- normative textを `docs/` へ重複コピーしない
- explanatory docの更新が必要な場合だけ対応する `docs/` を読む

## Stop condition

変更対象のrule、影響するcontract、必要なvalidationが特定できたら追加探索を止めます。
