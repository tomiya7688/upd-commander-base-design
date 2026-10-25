# UPD Commander Base Design

**日本語** | [English](README.en.md)

UPD Commander Base Design は、アプリケーションを **UI / Process / Data** の3層に分離し、各層の処理呼び出しを **Commander**、層間通信を **Messenger**、実際の処理を各層の処理モジュールへ分離するための基本設計です。

この設計は MVVM 等の既存パターン名をコード構造へそのまま持ち込むことを目的としません。UI・ゲーム処理・データ処理の責務と通信経路を明示し、依存方向を制御することを目的とします。

## この設計の本質

UPD Commander Base Design の大きな特徴は、規則に従って実装することで、**各クラス・モジュール・アクセス制御単位の責務が自然に小さくなる**ことです。

処理量が増えた場合でも、1つの Commander や処理モジュールへ処理を抱え込ませる必要はありません。処理を複数の処理モジュールや下位 Commander に分割し、上位 Commander はそれらを順番に呼び出して一連の処理を構成できます。

たとえば大きな処理が「読込 → 検証 → 変換 → 保存」で構成されるなら、それぞれを独立した処理として分離し、上位 Commander は各処理を適切な順序で呼び出すことだけを担当できます。これにより、処理全体が大きくなっても、上位 Commander 自身の責務は「その一連の処理を指揮すること」に合わせられます。

つまり UPD Commander Base Design を厳密に適用している場合、**「処理量が多いから1つのクラスやモジュールが巨大になる」という状態を避けやすくなります。** 処理量そのものは分割可能であり、複数の責務の協調は Commander の「呼び出しを指揮する」という1つの責務として表現できるためです。

この性質により、十分に大きなクラスやモジュールを見つけた場合、それを単なるコード量の多さではなく、**責務過多または責務分割不足を疑う強いシグナル**として扱えます。UPD に従った分割が十分であれば、実処理はより小さな単位へ移され、Commander はその呼び出し関係へ収束するためです。

したがって本設計では、責務分割を開発者の注意力だけに依存させるのではなく、構造そのものによって責務を小さく保ち、肥大化した単位を発見しやすい状態を作ることを重視します。

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

機能全体を1本の矢印に詰め込まず、層をまたぐ4つの区間に分けて示します。矢印は責務上の受け渡しを表し、特定の関数呼び出し順や同期方式を指定しません。

| 表記 | 意味 |
| --- | --- |
| `--[呼出]-->` | Commanderが同じ層のProcessingまたはMessengerへ依頼する |
| `--[結果返却: ...]-->` | Processingが結果をCommanderへ返す。追加データ要求も結果として返す |
| `==[メッセージ: ...]==>` | Messengerが要求または結果を隣接層のMessengerへ運ぶ |
| `--[受渡し]-->` | Messengerが受信内容を同じ層のCommanderへ渡す |

Messengerは直接メソッド呼び出しでも実装できます。メッセージの表記はキューや非同期処理を必須にしません。詳しくは[Data Flow](docs/data-flow.md)を参照してください。

### 1. UI → Process: 入力から処理を始める

```text
UI Commander --[呼出: 入力を読む]--> UI Processing
UI Processing --[結果返却: 入力値]--> UI Commander
UI Commander --[呼出]--> UI Messenger
UI Messenger ==[メッセージ: 要求]==> Process Messenger
Process Messenger --[受渡し]--> Process Commander
Process Commander --[呼出]--> Process Processing
```

### 2. Process → Data: 必要なデータを要求する

```text
Process Processing --[結果返却: 追加データ要求]--> Process Commander
Process Commander --[呼出]--> Process Messenger
Process Messenger ==[メッセージ: データ要求]==> Data Messenger
Data Messenger --[受渡し]--> Data Commander
Data Commander --[呼出]--> Data Processing
```

Processingは必要なデータを結果としてCommanderへ返します。Processing自身がMessengerを呼んだり、Dataへ直接要求したりするわけではありません。

### 3. Data → Process: 取得結果を返す

```text
Data Processing --[結果返却: 取得結果]--> Data Commander
Data Commander --[呼出]--> Data Messenger
Data Messenger ==[メッセージ: 取得結果]==> Process Messenger
Process Messenger --[受渡し]--> Process Commander
```

### 4. Process → UI: 最終結果を表示へ渡す

```text
Process Commander --[呼出: 処理を再開]--> Process Processing
Process Processing --[結果返却: 最終結果]--> Process Commander
Process Commander --[呼出]--> Process Messenger
Process Messenger ==[メッセージ: 最終結果]==> UI Messenger
UI Messenger --[受渡し]--> UI Commander
UI Commander --[呼出]--> UI Processing
```

### 一連の例: 商品検索

検索画面のキーワードから商品を検索し、結果一覧を表示する流れです。

1. UI Processingが入力欄からキーワードを読み、UI Commanderへ返します。
2. UI CommanderはUI Messengerへ検索要求を渡し、UI MessengerがProcess Messengerへ送ります。Process Messengerは要求をProcess Commanderへ渡し、Process CommanderがProcess Processingを呼びます。
3. 商品情報が必要な場合、Process Processingは追加データ要求を結果としてProcess Commanderへ返します。Process CommanderはProcess Messengerへ要求送信を依頼し、Process Messengerが要求をData Messengerへ送ります。
4. Data Messengerは要求をData Commanderへ渡し、Data CommanderがData Processingを呼びます。Data Processingが商品情報を返すと、Data CommanderはData Messengerへ応答送信を依頼します。Data Messengerが結果をProcess Messengerへ送り、Process MessengerがProcess Commanderへ渡します。
5. Process CommanderがProcess Processingを再開し、Process Processingが検索結果を返します。
6. Process CommanderはProcess Messengerへ最終結果の送信を依頼します。Process MessengerがUI Messengerへ送り、UI Messengerが結果をUI Commanderへ渡します。UI CommanderはUI Processingを呼んで一覧を表示します。

## ドキュメント

- [日本語ドキュメント執筆ガイド](docs/japanese-writing-guide.md) — 日本語文書の用語・規則強度・レビュー基準
- [Architecture](docs/architecture.md) — 全体構造と責務
- [Layer Rules](docs/layer-rules.md) — UI / Process / Data 各層の規則
- [Commander & Messenger](docs/commander-messenger.md) — Commander と Messenger の責務
- [Data Flow](docs/data-flow.md) — 要求と返却の流れ
- [Anti-patterns](docs/anti-patterns.md) — 禁止・非推奨構造
- [Technical Specification](specification/README.md) — 実装時に従う規定
- [Third-Party Notices](THIRD_PARTY_NOTICES.md) — 配布チェッカーが含む第三者コンポーネントのライセンス

## スコープ

UPD Commander Base Design は、設計規約とそれを補助・検証する checker / support tools のためのプロジェクトです。

コンテキスト削減、AI向けread routing、探索停止、Context Pack生成そのものを目的とする仕組みではありません。

他の開発支援手法やAI向けツールがこの設計の責務境界を利用することはできますが、それらはUPD Commander本体とは別の関心事として扱います。

## 最重要ルール

> **Commander は処理を行わない。Commander は適切な処理を呼び出す。Messenger は層を越える通信を行い、実処理は各層の処理モジュールが担当する。**

この規則を破って Commander や Messenger が肥大化した場合、その処理は別の処理モジュールへ分離する必要があります。
