# UPD Commander Technical Specification

**日本語** | [English](README.en.md)

このフォルダは UPD Commander 設計の詳細な技術規定を格納するための領域です。

`docs/` が設計の説明・概要を扱うのに対し、`specification/` は実装時に従う規定そのものを扱います。

## 規定文書

- [Layer Specification](layer-spec.md) — UI / Process / Data 各層の責務
- [Application Boundary](application-boundary.md) — Application / Sub Application の境界と3層構造の再帰適用
- [Commander Specification](commander-spec.md) — Commander の責務と禁止事項
- [Messenger Specification](messenger-spec.md) — 層間通信の責務と正式経路
- [Compresser Specification](compresser-spec.md) — 複数の引数・返却値をクラス単位の Container としてまとめる責務
- [Compresser / Container Checker Rules](compresser-check-rules.md) — Container 化の Attention / Warning 判定と Self Check 規定
- [Responsibility Check Rules](responsibility-check-rules.md) — UPD401 の共通責務単位・250行 / 12メソッド境界
- [Processing Specification](processing-spec.md) — 各層の実処理の責務
- [Dependency Rules](dependency-rules.md) — 許可・禁止される依存関係
- [Data Commander Communication](data-commander-communication.md) — Data Commander 同士の直接通信に対する警告規定
- [Message Contract](message-contract.md) — 層間メッセージの契約
- [Error Handling](error-handling.md) — エラー検出・伝播・変換
- [Testing Rules](testing-rules.md) — 単体・統合・アーキテクチャテスト規定
- [Recommended Practices](recommended-practices.md) — 1ファイル1責務、1モジュール1責務、1関数1動作、コメント、閉じた処理モジュール等の推奨規約
- [Implementation Quality Requirements](implementation-quality.md) — インデント、ビルド、CI、formatter、linter、テスト等の最低品質条件

## 最上位規定

1. システムは原則として UI / Process / Data の3層に分離する。
2. 独立した機能境界は Application として扱い、各 Application 内で UI / Process / Data の3層を再度適用する。
3. 別 Application の内部実装へ直接依存せず、Messenger / Contract / 上位 Commander 等の明示された境界を利用する。
4. Commander は処理を実行せず、適切な Processing または Messenger を呼び出す。
5. Messenger は層を越える通信のみを担当する。
6. 複数の通信値をまとめる必要がある場合、Compresser はそれらを単一の通信単位へ変換し、Commander / Messenger は原則としてその内部の業務的意味を解釈しない。
7. 実際の計算・変換・描画・データ操作は各層の Processing が担当する。
8. 別層の Processing を直接呼び出してはならない。
9. UI と Data は直接通信してはならない。
10. Process は UI の表示方法を知らず、Data の保存方式を知らない。
11. UI フレームワークや DB 等の層固有型を境界越しに漏らさない。
12. 設計上の責務単位は言語固有のクラスではなく、ファイルおよびモジュールを基本とする。
13. クラスを使用する言語では、クラスをモジュール内部の実装手段として使用してよいが、ファイルまたはモジュールの責務境界を曖昧にしてはならない。

## 規定の強さ

本文中の「しなければならない」「禁止する」は原則として必須規定です。

「推奨する」「してよい」は、プロジェクト固有事情に応じて変更可能です。ただし、その変更によって最上位規定を破ってはなりません。

Container 化は可読性改善を目的とする推奨規定であり、一般プロジェクトでは一律必須とはしません。パッケージ化自体のコストも考慮し、個別の未Container化は Attention、大幅な Commander / Messenger 圧縮が見込める場合のみ Warning とします。

`implementation-quality.md` は推奨事項ではなく、通常品質の実装として受け入れるための適合条件を定義します。

規定外の例外を導入する場合は、理由・範囲・代替案・将来除去可能性を文書化してください。
