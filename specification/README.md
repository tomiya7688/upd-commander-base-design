# UPD Commander 技術仕様

**日本語** | [English](README.en.md)

用語の意味は[日本語用語集](../docs/glossary.md)で確認できます。

この索引は、UPD Commander の実装規則を確認するための入口です。設計の考え方や背景を知りたい場合は、[Architecture](../docs/architecture.md) から読んでください。ここでは規則の要約を重ねず、各規則の正本と読み進め方を案内します。

## 読者別の読み方

### UPDを初めて読む

先に小さな機能を動かしたい場合は、[最小UPDチュートリアル](../docs/minimal-tutorial.md)へ進んでください。入力・検証・保存・表示の配置からCheckerの実行までを体験できます。

1. [Architecture](../docs/architecture.md) — [UI](../docs/glossary.md#ui) / [Process](../docs/glossary.md#process) / [Data](../docs/glossary.md#data) と各[役割](../docs/glossary.md#role)の全体像
2. [Layer Specification](layer-spec.md) — [3層](../docs/glossary.md#layer)それぞれが担当すること
3. [Commander Specification](commander-spec.md) — [Commander](../docs/glossary.md#commander)が呼び出しをどう整理するか
4. [Messenger Specification](messenger-spec.md) — 層を越える通信の経路
5. [Guideline Levels](guideline-levels.md) — 必須・推奨・小技の違い

まずはこの5文書を読めば、基本構造と規則の強さを確認できます。個々の規則を実装へ適用するときは、以下から該当する正本を参照してください。

### Checkerを実装・保守する

1. [Guideline Levels](guideline-levels.md) — 規則の強さを分類する[契約](../docs/glossary.md#contract)
2. [Checker Severity Boundary](checker-severity.md) — 規則と[Checker](../docs/glossary.md#checker) [severity](../docs/glossary.md#severity)の対応
3. [Checker Regression Matrix](checker-regression-matrix.md) — Checker間で共有する回帰期待値
4. 実装対象の「判定・契約」文書 — 各[rule](../docs/glossary.md#rule)の入力、判定、出力
5. [Implementation Quality Requirements](implementation-quality.md) — 実装とCIの品質条件

### 規則を確認する

以下の一覧から確認したい領域を選びます。Checker利用者は「Checker判定」、実装者は「構造と通信」または「Checkerの契約」から該当文書へ進んでください。

## 仕様文書

### 構造と通信

- [Layer Specification](layer-spec.md) — UI / Process / Data の[責務](../docs/glossary.md#responsibility)
- [Application Boundary](application-boundary.md) — [Application](../docs/glossary.md#application)[境界](../docs/glossary.md#boundary)と3層構造の再帰適用
- [Common / Shared Specification](common-shared-spec.md) — 層中立共有領域の責務と依存方向
- [Commander Specification](commander-spec.md) — Commanderの役割と禁止事項
- [Messenger Specification](messenger-spec.md) — 層間通信の役割と正式経路
- [Processing Specification](processing-spec.md) — 各層の[実処理](../docs/glossary.md#processing)の責務
- [Dependency Rules](dependency-rules.md) — 許可・禁止される依存関係
- [Data Commander Communication](data-commander-communication.md) — Data Commander間通信の警告条件
- [Message Contract](message-contract.md) — 層間[メッセージ](../docs/glossary.md#message)の契約
- [Error Handling](error-handling.md) — エラーの検出・伝播・変換

### 規則の強さとChecker判定

- [Guideline Levels](guideline-levels.md) — 必須・推奨・小技の分類
- [Checker Severity Boundary](checker-severity.md) — [Error](../docs/glossary.md#error) / [Warning](../docs/glossary.md#warning) / [Attention](../docs/glossary.md#attention)の意味と対応
- [Responsibility Check Rules](responsibility-check-rules.md) — 責務単位とUPD401–UPD404の判定契約
- [Flat Layer Checker Rules](flat-layer-check-rules.md) — 大規模で平坦な層のUPD405判定契約
- [Model Attention Checker Rules](model-attention-check-rules.md) — 値グループに対するUPD406判定契約
- [Compresser / Container Checker Rules](compresser-check-rules.md) — [Container](../docs/glossary.md#container)化の判定契約
- [Finding Baseline Contract](baseline-contract.md) — [Finding](../docs/glossary.md#finding) fingerprintとbaseline JSON契約
- [Checker Regression Matrix](checker-regression-matrix.md) — Checker共通の回帰期待値

### 推奨設計と通常品質

- [Compresser Specification](compresser-spec.md) — 値をContainerへまとめる推奨設計
- [Recommended Practices](recommended-practices.md) — 責務分割、配置、実装例の推奨
- [Testing Rules](testing-rules.md) — テスト種別と設計規則
- [Implementation Quality Requirements](implementation-quality.md) — ビルド、CI、formatter、linter等の通常品質

## 仕様の読み分け

各文書に記載された規則の強さと適用範囲が、その文書の正本です。必須・推奨・小技の分類は [Guideline Levels](guideline-levels.md)、Checker severityとの対応は [Checker Severity Boundary](checker-severity.md) を参照してください。

この索引は規則の重複要約や別の判断順序を定義しません。実装時は該当する仕様本文を確認し、推奨事項を採用しない場合はプロジェクトの判断理由を記録してください。
