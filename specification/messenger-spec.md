# Messenger Specification

Messenger は UPD Commander における層間通信専用コンポーネントである。

## 1. 基本原則

1. Messenger は層を越える要求・返却を運ぶ。
2. Messenger は実処理を持ってはならない。
3. Messenger は受信した内容を対象層の Messenger または Commander へ渡す。
4. Messenger はゲームルール、UI 表示ルール、保存ルールを判断してはならない。
5. Messenger は通信形式の変換など、通信成立に必要な最小限の処理のみ許可する。
6. 複数の引数・返却値が存在する場合、それらを個別に理解して運ぶのではなく、Compresser により単一の通信単位へまとめて扱うことを推奨する。
7. Messenger は Compresser が生成した payload 内部の業務的意味を原則として解釈してはならない。

## 2. Messenger が行ってよいこと

- メッセージ送信
- メッセージ受信
- 宛先の識別
- コマンド種別の付与・解釈
- request_id / correlation_id 等の通信識別子管理
- 同期 / 非同期送信
- 通信失敗の検出
- 通信契約に沿った最低限のラップ / アンラップ
- Compresser によって生成された単一の通信単位の配送

## 3. Messenger が行ってはならないこと

- ダメージ計算
- UI 表示判断
- HP ゲージ比率計算
- ファイル内容の意味上の整形
- セーブデータ生成
- ゲーム状態更新
- DB クエリ組み立てを含む Data Processing
- Process Processing の代替
- payload 内部の個々の値を読み、その意味に応じて業務判断すること

## 4. 正式な通信経路

基本経路は以下とする。

```text
UI Messenger <-> Process Messenger <-> Data Messenger
```

UI Messenger と Data Messenger の直接通信は禁止する。

## 5. 送信方向

要求:

```text
UI Commander
  -> UI Messenger
  -> Process Messenger
  -> Process Commander
```

データ要求:

```text
Process Commander
  -> Process Messenger
  -> Data Messenger
  -> Data Commander
```

返却:

```text
Data Commander
  -> Data Messenger
  -> Process Messenger
  -> Process Commander
```

```text
Process Commander
  -> Process Messenger
  -> UI Messenger
  -> UI Commander
```

## 6. メッセージ内容

Messenger は原則としてフレームワーク非依存・層外実装非依存の値を運ぶ。

避けるべき例:

- pygame.Event を Process Layer へ送る
- DB Connection を Process Layer へ渡す
- ファイルハンドルを UI Layer へ返す

推奨例:

```text
{
  command: "move_player",
  payload: {
    direction: "left"
  }
}
```

```text
{
  command: "load_character",
  payload: {
    character_id: "abc123"
  }
}
```

複数値を扱う場合でも Messenger の引数を増やすのではなく、可能な限り Message Contract に従った単一の通信単位へまとめる。

パッケージ化・展開そのものの責務は `compresser-spec.md` に従う。

## 7. 通信方式

UPD Commander は通信手段を固定しない。

以下はいずれも実装可能とする。

- 直接メソッド呼び出し
- イベントキュー
- メッセージキュー
- シグナル
- callback
- async / await
- IPC
- ネットワーク通信

ただし、どの方式でも責務境界は同じでなければならない。
