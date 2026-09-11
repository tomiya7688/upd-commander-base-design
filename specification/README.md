# UPD Commander Technical Specification

このフォルダは UPD Commander 設計の詳細な技術規定を格納するための領域です。

`docs/` が設計の説明・概要を扱うのに対し、`specification/` は実装時に従う規定そのものを扱います。

## 規定文書

- [Layer Specification](layer-spec.md) — UI / Process / Data 各層の責務
- [Commander Specification](commander-spec.md) — Commander の責務と禁止事項
- [Messenger Specification](messenger-spec.md) — 層間通信の責務と正式経路
- [Processing Specification](processing-spec.md) — 各層の実処理の責務
- [Dependency Rules](dependency-rules.md) — 許可・禁止される依存関係
- [Message Contract](message-contract.md) — 層間メッセージの契約
- [Error Handling](error-handling.md) — エラー検出・伝播・変換
- [Testing Rules](testing-rules.md) — 単体・統合・アーキテクチャテスト規定
- [Recommended Practices](recommended-practices.md) — 1クラス1責務、1関数1動作、コメント、閉じた処理クラス等の推奨規約
- [Implementation Quality Requirements](implementation-quality.md) — インデント、ビルド、CI、formatter、linter、テスト等の最低品質条件

## 最上位規定

1. システムは原則として UI / Process / Data の3層に分離する。
2. Commander は処理を実行せず、適切な Processing または Messenger を呼び出す。
3. Messenger は層を越える通信のみを担当する。
4. 実際の計算・変換・描画・データ操作は各層の Processing が担当する。
5. 別層の Processing を直接呼び出してはならない。
6. UI と Data は直接通信してはならない。
7. Process は UI の表示方法を知らず、Data の保存方式を知らない。
8. UI フレームワークや DB 等の層固有型を境界越しに漏らさない。

## 規定の強さ

本文中の「しなければならない」「禁止する」は原則として必須規定です。

「推奨する」「してよい」は、プロジェクト固有事情に応じて変更可能です。ただし、その変更によって最上位規定を破ってはなりません。

`implementation-quality.md` は推奨事項ではなく、通常品質の実装として受け入れるための適合条件を定義します。

規定外の例外を導入する場合は、理由・範囲・代替案・将来除去可能性を文書化してください。
