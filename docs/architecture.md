# Architecture

## 1. 目的

UPD Commander Base Design は、UI・処理・データの責務を分離し、処理経路を追跡しやすくするための設計です。

対象システムを次の3層に分けます。

- **UI Layer**: 入力受付、表示判断、描画などUIに関する責務
- **Process Layer**: ゲームロジックやアプリケーション上の計算
- **Data Layer**: データ取得、保存、読み込み、整形

## 2. 共通構成

各層は原則として次の3種類の責務を持ちます。

### Commander

その層で「次に何を呼ぶか」を決めます。

Commander 自身は、計算・描画・保存・変換などの実処理を持ちません。

### Messenger

隣接する層との通信を担当します。

Messenger 自身は、ゲームロジックや表示ロジックなどの判断を持ちません。

### Processing

その層に属する実際の処理を担当します。

例:

- UI Processing: 表示形式の決定、描画、入力のUI上の解釈
- Process Processing: ダメージ計算、ターン処理、ルール判定
- Data Processing: 読み込み、保存、変換、整形

## 3. 依存関係

基本の通信経路は以下です。

```text
UI ↔ Process ↔ Data
```

原則として次のような層越えは禁止します。

```text
UI → Data
Data → UI
```

また、他層の Processing を直接呼び出してはいけません。

```text
UI Processing → Process Processing  # NG
Process Processing → Data Processing # NG
```

層を越える場合は Messenger を経由します。

## 4. UI表示責務

Process 層から返された値を「どう表示するか」は UI 層の責務です。

例として Process 層が次を返したとします。

```text
hp = 24
max_hp = 100
```

Process 層は以下を判断しません。

- HPバーの幅
- 文字列を `24 / 100` とするか
- 危険時に赤くするか
- 点滅させるか

これらは UI Processing が決めます。

したがって UPD Commander Base Design では、既存アーキテクチャにおける ViewModel のような名称を正式な構造名として使用する必要はありません。

## 5. 設計上の狙い

- UIフレームワーク依存をUI層へ閉じ込める
- Process層を単体テストしやすくする
- Data形式や保存先の変更をゲームロジックから分離する
- 呼び出し経路を一定にする
- CommanderやMessengerの肥大化を防止する
- 他言語・他ゲームエンジンへ移植しやすくする
