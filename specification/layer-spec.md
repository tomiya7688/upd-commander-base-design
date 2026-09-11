# Layer Specification

本書は UPD Commander における `UI` / `Process` / `Data` の3層の責務を規定する。

## 1. 共通規定

1. システムは原則として `UI Layer`、`Process Layer`、`Data Layer` に分離しなければならない。
2. 各層は `Commander`、必要な `Messenger`、実処理を担当する Processing 群から構成する。
3. ある層の Processing が別層の Processing を直接呼び出してはならない。
4. 層を越える要求・返却は Messenger を経由しなければならない。
5. Commander と Messenger に実処理を実装してはならない。
6. フレームワーク固有依存は、それを必要とする層の内部へ閉じ込める。

## 2. UI Layer

UI Layer は利用者との入出力と表示上の処理を担当する。

### 2.1 UI Layer が担当するもの

- キーボード、マウス、ゲームパッド、タッチ等の入力取得
- ウィンドウ、画面、画面部品の生成
- 描画
- アニメーション、画面遷移、表示上の演出
- Process Layer から返された値を表示用の値・状態へ変換する処理
- 表示上だけに意味を持つ判定
- UI フレームワーク固有処理

例:

- `hp=24`, `max_hp=100` を `24 / 100` という文字列へ変換する
- HP 比率からゲージ幅を計算する
- HP が一定割合以下なら警告表示を有効にする
- pygame の Surface、Rect、Event を扱う

### 2.2 UI Layer が担当してはならないもの

- ダメージ計算
- 経験値計算
- AI 判断
- ゲームルール上の勝敗判定
- セーブデータの直接読み書き
- DB、ファイル、外部永続化先への直接アクセス

### 2.3 UI 固有型

pygame 等の UI フレームワーク固有型は、原則として UI Layer の外へ渡してはならない。

Process Layer に送る前に、UI Messenger が扱えるフレームワーク非依存の値またはメッセージへ変換する。

## 3. Process Layer

Process Layer はアプリケーションの意味上の処理、規則、計算を担当する。

### 3.1 Process Layer が担当するもの

- ゲームロジック
- 業務ロジック
- 数値計算
- 状態遷移
- 判定
- UI や保存形式に依存しないデータ処理
- Data Layer から受け取ったデータを用いた計算

### 3.2 Process Layer が担当してはならないもの

- pygame 等による描画
- 表示文字列や表示色など UI 固有の決定
- ファイルパスや DB 接続方法など Data Layer 固有の知識
- 保存形式固有のシリアライズ処理

### 3.3 データが必要な場合

Process Processing は Data Layer を直接呼び出してはならない。

処理に追加データが必要なことが判明した場合、Process Commander がその要求を受け、Process Messenger を通じて Data Layer へ要求する。

返却されたデータは Process Messenger が受信し、Process Commander が適切な Process Processing へ渡す。

## 4. Data Layer

Data Layer はデータの取得、保存、および保存・取得に必要な整形を担当する。

### 4.1 Data Layer が担当するもの

- ファイル読み書き
- DB 読み書き
- 外部ストレージへのアクセス
- データ取得
- データ保存
- シリアライズ / デシリアライズ
- 保存形式から内部受け渡し形式への変換
- データ取得時・保存時に必要な整形

### 4.2 Data Layer が担当してはならないもの

- UI 表示方法の判断
- ゲームルール上の計算
- UI イベント処理
- Process Layer の業務判断

Data Layer は「その値をゲーム上どう使うか」を知らなくてよい。

## 5. 層の境界

以下を正式な境界とする。

```text
UI <-> Process <-> Data
```

`UI <-> Data` の直接通信は禁止する。

UI がデータ保存を必要とする場合でも、UI は Process Layer に要求し、Process Layer が保存すべき意味を判断した上で Data Layer に要求する。

## 6. 返却方向

要求の返却も要求時と同じ境界を守る。

```text
Data -> Data Messenger -> Process Messenger -> Process Commander -> Process Processing
Process -> Process Messenger -> UI Messenger -> UI Commander -> UI Processing
```

Data Layer から UI Layer へ直接結果を返してはならない。
