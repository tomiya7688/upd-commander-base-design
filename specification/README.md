# UPD Commander Technical Specification

このフォルダは UPD Commander 設計の詳細な技術規定を格納するための領域です。

## 目的

- UI / Process / Data 各層の責務を明文化する
- Commander / Messenger / Processing の責務境界を定義する
- 許可される依存関係と禁止される依存関係を規定する
- 実装言語やフレームワークに依存しない設計規約として参照可能にする
- 各プロジェクトで同一の設計判断を再利用できるようにする

## 想定する規定文書

- `layer-spec.md` : UI / Process / Data 各層の技術規定
- `commander-spec.md` : Commander の技術規定
- `messenger-spec.md` : Messenger の技術規定
- `processing-spec.md` : 各層の Processing の技術規定
- `dependency-rules.md` : 許可・禁止される依存方向
- `message-contract.md` : 層間メッセージの契約
- `error-handling.md` : エラー伝播・失敗時処理
- `testing-rules.md` : テスト時の分離・モック方針

現在の `docs/` は設計の説明・概要を扱い、この `specification/` は実装時に従う技術規定そのものを扱います。
