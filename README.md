# UPD Commander Base Design

UPD Commander Base Design は、アプリケーションを **UI / Process / Data** の3層に分離し、各層の処理呼び出しを **Commander**、層間通信を **Messenger**、実際の処理を各層の処理モジュールへ分離するための基本設計です。

この設計は MVVM 等の既存パターン名をコード構造へそのまま持ち込むことを目的としません。UI・ゲーム処理・データ処理の責務と通信経路を明示し、依存方向を制御することを目的とします。

## 基本原則

1. システムを `UI` / `Process` / `Data` の3層に分ける。
2. Commander は実処理を持たず、適切な処理を呼び出す。
3. Messenger は層間通信を担当し、業務処理・ゲーム処理を持たない。
4. 実際の計算・変換・描画・データ操作は各層の処理モジュールが担当する。
5. 層を飛び越える直接アクセスを原則禁止する。
6. Process 層は UI の表示方法を知らない。
7. UI 層は Data 層へ直接アクセスしない。
8. Data 層は UI やゲームロジックを知らない。

## 基本構造

```text
UI Layer
├─ Commander
├─ Messenger
└─ UI Processing

Process Layer
├─ Commander
├─ Messenger
└─ Process Processing

Data Layer
├─ Commander
├─ Messenger
└─ Data Processing
```

## 基本フロー

```text
UI Input
  ↓
UI Commander
  ↓
UI Processing / UI Messenger
  ↓
Process Messenger
  ↓
Process Commander
  ↓
Process Processing
  ↓
必要なら Data Messenger
  ↓
Data Commander
  ↓
Data Processing
  ↓
Data Messenger
  ↓
Process Messenger
  ↓
Process Commander
  ↓
Process Processing
  ↓
Process Messenger
  ├─→ UI Messenger → UI Commander → UI Processing
  └─→ Data Messenger → Data Commander → Data Processing
```

## ドキュメント

- [Architecture](docs/architecture.md) — 全体構造と責務
- [Layer Rules](docs/layer-rules.md) — UI / Process / Data 各層の規則
- [Commander & Messenger](docs/commander-messenger.md) — Commander と Messenger の責務
- [Data Flow](docs/data-flow.md) — 要求と返却の流れ
- [Anti-patterns](docs/anti-patterns.md) — 禁止・非推奨構造
- [Technical Specification](specification/README.md) — 実装時に従う規定

## スコープ

UPD Commander Base Design は、設計規約とそれを補助・検証する checker / support tools のためのプロジェクトです。

コンテキスト削減、AI向けread routing、探索停止、Context Pack生成そのものを目的とする仕組みではありません。

他の開発支援手法やAI向けツールがこの設計の責務境界を利用することはできますが、それらはUPD Commander本体とは別の関心事として扱います。

## 最重要ルール

> **Commander は処理を行わない。Commander は適切な処理を呼び出す。Messenger は層を越える通信を行い、実処理は各層の処理モジュールが担当する。**

この規則を破って Commander や Messenger が肥大化した場合、その処理は別の処理モジュールへ分離する必要があります。
