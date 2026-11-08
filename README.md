# UPD Commander Base Design

**日本語** | [English](README.en.md)

用語の意味は[日本語用語集](docs/glossary.md)で確認できます。

UPD Commander Base Design は、アプリケーションを **[UI](docs/glossary.md#ui) / [Process](docs/glossary.md#process) / [Data](docs/glossary.md#data)** の[3層](docs/glossary.md#layer)に分離し、各層の処理呼び出しを **[Commander](docs/glossary.md#commander)**、層間通信を **[Messenger](docs/glossary.md#messenger)**、実際の処理を各層の[処理モジュール](docs/glossary.md#processing)へ分離するための基本設計です。

この設計は MVVM 等の既存パターン名をコード構造へそのまま持ち込むことを目的としません。UI・ゲーム処理・データ処理の[責務](docs/glossary.md#responsibility)と通信経路を明示し、依存方向を制御することを目的とします。

## どんな開発に向いているか

UPDは万能な設計ではありません。規模だけで採否を決めるのではなく、分離の利益が、構造を作り維持するコストに見合うかを検討してください。

- **候補になりやすい:** 中規模以上、または今後成長し、入力・業務やゲームの処理・保存が増えるアプリケーション。複数人での分担や長期保守、画面・処理・保存先を別々に変更したい場合にも向いています。
- **過剰になりやすい:** 数ファイルで完結する短命なスクリプトや試作品。役割や通信経路を設ける手間が、分離の利益を上回ることがあります。
- **慎重な判断が必要:** 既存のアーキテクチャが強く固定されたシステムや、極端な性能制約のある箇所。全面移行のコストや、分割・通信単位の追加による影響を先に評価してください。

## 長所と短所

| 期待できること                                                                                                            | 支払うコスト・注意点                                                                                                  |
| ------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| UI・Process・Dataの責務や依存を分け、呼び出し経路や肥大化した責務を追いやすくする                                         | Commander・Messengerを経由する分、単純な直接呼び出しよりコード量が増える場合がある                                    |
| 境界を適切に設計すれば、実処理を分けて変更影響を局所化し、ProcessをUI・Dataの具体的な実装から切り離してテストしやすくなる | 境界を先に設計する必要がある。Application / Sub ApplicationやCommonの切り方を誤ると、かえって複雑になる               |
| 言語やOOP・非OOPに依存せず設計でき、対応するCheckerで一部の設計規則を継続的に検査できる                                   | 用語と規則の学習が必要。Checkerは設計の正しさをすべて保証せず、言語ごとの対応範囲も異なる                             |
| 必須の責務分離と、可読性のための推奨構造を分けて判断できる                                                                | 性能が厳しい箇所では、必須ルールを守ったうえで、推奨のファイル分割やContainer化を採用しない判断が必要になる場合がある |

変更の局所化やテスト容易性は、適切な境界設計と実装によって得られる効果であり、自動的に保証されるものではありません。[必須・推奨・任意の区別](specification/guideline-levels.md)も参照してください。

## 導入するならいつか

**新規開発の初期、または機能の境界がまだ小さい段階**は、責務と依存方向を最初から揃えやすいため導入しやすい時期です。

開発途中からの導入は不可能ではありませんが、負担が大きくなりがちです。UIからDataへの直接依存を解き、業務処理をProcessへ、I/OをDataへ移し、Commander・Messengerの経路を整えるほか、frameworkやDBの型・DTOの境界、テスト、参照・importも見直す必要があります。既存システムでは全面書き換えを前提にせず、Applicationやfeature単位で段階的に導入できます。対象機能の入出力、既存側とUPD側の責務・依存方向、移行を終える基準を先に決め、接続とテストを確認してください。

## 最小構造の見取り図

UI（入力・表示）・Process（業務やゲームの処理）・Data（保存・取得）の3層を分けます。各層では、Commanderが呼び出しを指揮し、Processingが実際の処理を担当します。層をまたぐ受け渡しはMessengerが担当します。

```text
UI:      Commander / Processing / Messenger
Process: Commander / Processing / Messenger
Data:    Commander / Processing / Messenger
```

これは役割の配置図であり、矢印による依存方向や関数の実行順序、非同期通信を指定するものではありません。層間の依存・通信経路は[依存ルール](specification/dependency-rules.md)、具体的な入力から保存・表示までの流れは[最小UPDチュートリアル](docs/minimal-tutorial.md)を参照してください。

## この設計の本質

UPD Commander Base Design の大きな特徴は、規則に従って実装することで、**各クラス・[モジュール](docs/glossary.md#module)・アクセス制御単位の責務が自然に小さくなる**ことです。

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

| 表記                     | 意味                                                              |
| ------------------------ | ----------------------------------------------------------------- |
| `--[呼出]-->`            | Commanderが同じ層のProcessingまたはMessengerへ依頼する            |
| `--[結果返却: ...]-->`   | Processingが結果をCommanderへ返す。追加データ要求も結果として返す |
| `==[メッセージ: ...]==>` | Messengerが要求または結果を隣接層のMessengerへ運ぶ                |
| `--[受渡し]-->`          | Messengerが受信内容を同じ層のCommanderへ渡す                      |

Messengerは直接メソッド呼び出しでも実装できます。[メッセージ](docs/glossary.md#message)の表記はキューや非同期処理を必須にしません。詳しくは[Data Flow](docs/data-flow.md)を参照してください。

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

- [最小UPDチュートリアル](docs/minimal-tutorial.md) — 入力・検証・保存・表示を作り、Checkerを一度実行する
- [日本語ドキュメント執筆ガイド](docs/japanese-writing-guide.md) — 日本語文書の用語・規則強度・レビュー基準
- [Architecture](docs/architecture.md) — 全体構造と責務
- [Layer Rules](docs/layer-rules.md) — UI / Process / Data 各層の規則
- [Commander & Messenger](docs/commander-messenger.md) — Commander と Messenger の責務
- [Data Flow](docs/data-flow.md) — 要求と返却の流れ
- [Anti-patterns](docs/anti-patterns.md) — 禁止・非推奨構造
- [Technical Specification](specification/README.md) — 実装時に従う規定
- [Third-Party Notices](THIRD_PARTY_NOTICES.md) — 配布チェッカーが含む第三者コンポーネントのライセンス
- [配布物のセキュリティ](docs/release-security-design.md) — 現行公開状況、署名・attestation、公開ゲート

## スコープ

UPD Commander Base Design は、設計規約とそれを補助・検証する [checker](docs/glossary.md#checker) / support tools のためのプロジェクトです。

コンテキスト削減、AI向けread routing、探索停止、Context Pack生成そのものを目的とする仕組みではありません。

他の開発支援手法やAI向けツールがこの設計の責務[境界](docs/glossary.md#boundary)を利用することはできますが、それらはUPD Commander本体とは別の関心事として扱います。

## 最重要ルール

> **Commander は処理を行わない。Commander は適切な処理を呼び出す。Messenger は層を越える通信を行い、実処理は各層の処理モジュールが担当する。**

この規則を破って Commander や Messenger が肥大化した場合、その処理は別の処理モジュールへ分離する必要があります。
