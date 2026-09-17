# UPD Commander Technical Specification

**日本語** | [English](README.en.md)

このフォルダは UPD Commander 設計の詳細な技術規定を格納するための領域です。

`docs/` が設計の説明・概要を扱うのに対し、`specification/` は実装時に従う規定そのものを扱います。

UPD Commander は特定の実装形を強制するフレームワークではなく、UI / Process / Data の責務と通信経路を整理するための設計思想です。規則の強さは [Guideline Levels](guideline-levels.md) に従い、**必須 / 推奨 / 小技**へ分類します。Checker の severity はこの分類に対応し、[Checker Severity Boundary](checker-severity.md) を正本とします。

## 規定文書

- [Guideline Levels](guideline-levels.md) — UPD の規則を必須 / 推奨 / 小技に分類する基準
- [Checker Severity Boundary](checker-severity.md) — 必須→Error / 推奨→Warning / 小技→Attention の対応と既存 rule マッピング
- [Layer Specification](layer-spec.md) — UI / Process / Data 各層の責務
- [Application Boundary](application-boundary.md) — Application / Sub Application の境界と3層構造の再帰適用
- [Commander Specification](commander-spec.md) — Commander の責務と禁止事項
- [Messenger Specification](messenger-spec.md) — 層間通信の責務と正式経路
- [Compresser Specification](compresser-spec.md) — 複数の引数・返却値を Container としてまとめる責務
- [Compresser / Container Checker Rules](compresser-check-rules.md) — Container 化の Attention / Warning 判定と Self Check 規定
- [Responsibility Check Rules](responsibility-check-rules.md) — UPD401 の共通責務単位・250行 / 12メソッド境界
- [Processing Specification](processing-spec.md) — 各層の実処理の責務
- [Dependency Rules](dependency-rules.md) — 許可・禁止される依存関係
- [Data Commander Communication](data-commander-communication.md) — Data Commander 同士の直接通信に対する警告規定
- [Message Contract](message-contract.md) — 層間メッセージの契約
- [Error Handling](error-handling.md) — エラー検出・伝播・変換
- [Testing Rules](testing-rules.md) — 単体・統合・アーキテクチャテスト規定
- [Recommended Practices](recommended-practices.md) — 1ファイル1責務、1モジュール1責務、1関数1動作、Namespace配置等の推奨規約
- [Implementation Quality Requirements](implementation-quality.md) — インデント、ビルド、CI、formatter、linter、テスト等の通常品質条件

## 必須 — UPD Core Rules

次の項目は UPD として成立するための最低条件です。

1. システムは原則として UI / Process / Data の3つの責務領域へ分離する。
2. UI は Data へ直接アクセスせず、層を越える依存経路を明示する。
3. Commander は処理の交通整理を担当し、独立した業務処理・ゲーム処理・保存処理を抱え込まない。
4. Messenger / Communicator は層間通信を担当し、その内部へ独立した実処理を混在させない。
5. 実際の計算・変換・描画・データ操作は担当する層へ置く。
6. 別層の Processing を直接呼び出さない。
7. Process は UI の表示方法を知らず、Data の保存方式を知らない。
8. Data は UI や業務・ゲームロジックを知らない。
9. UI フレームワークや DB 等の層固有型を、不必要に境界越しへ漏らさない。

これらを満たす限り、パッケージ、ファイル、クラス、関数の具体的な分割方法は使用言語・性能要件・プラットフォーム事情に応じて変更できます。

例えば性能が厳しい環境では、UI / Process / Data だけを物理的に分け、Commander にルーティング用メソッドをまとめ、Messenger / Communicator に通信メソッドをまとめる平坦な構造でも UPD として成立します。

## 推奨 — Recommended Structure

次の項目は保守性・探索性・テスト容易性を高めるために推奨しますが、UPD の成立条件ではありません。

- 1ファイル1責務
- 1モジュール1責務
- クラスを使用する場合の1クラス1責務
- 1関数1動作
- Processing を可能な範囲で閉じた単位にする
- Namespace / package / module とフォルダ構成を対応させる
- 大規模システムで Application / Sub Application 単位へ再帰的に分離する
- Commander / Messenger が肥大化する場合に Compresser / Container を利用する

性能、メモリ、呼び出しコスト、キャッシュ局所性、ビルド方式、プラットフォーム制約、既存資産との互換性等と競合する場合は、推奨事項を採用しない判断ができます。

## 小技 — Practical Tips

次の項目は設計を守るための必須条件ではなく、実装・レビュー・運用を楽にするためのテクニックです。

- 処理の目的や区切りをコメントで示す
- 大きなファイル・型・モジュールを責務過多の兆候としてレビューする
- 言語ごとの自然な module / package / class / free function 表現を使う
- 性能が厳しい場合は推奨構造を平坦化する
- Checker の Attention / Warning を設計判断の材料として使い、必要なら理由付き Ignore を利用する

詳細は [Guideline Levels](guideline-levels.md) を参照してください。

## 規定の強さ

本文中の「しなければならない」「禁止する」は、原則として UPD Core Rules またはその具体化に対して使用します。

「推奨する」「してよい」は、プロジェクト固有事情に応じて変更可能です。特に性能へ影響しうる構造上のルールは原則として推奨に留めます。

Container 化は可読性改善を目的とする推奨規定であり、一般プロジェクトでは一律必須とはしません。パッケージ化自体のコストも考慮し、個別の未Container化は Attention、大幅な Commander / Messenger 圧縮が見込める場合のみ Warning とします。

`implementation-quality.md` は UPD の成立条件とは別に、通常品質の実装として受け入れるための品質条件を定義します。

規定外の例外を導入する場合は、理由・範囲・代替案・将来除去可能性を文書化してください。

## 判断順序

```text
1. UPD Core Rules を守る
2. 性能・メモリ・プラットフォーム制約を満たす
3. 可能な範囲で Recommended Structure を採用する
4. Practical Tips で可読性・運用性を改善する
```

UPD の目的は、設計のために性能を犠牲にすることではありません。
