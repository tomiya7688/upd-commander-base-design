# Commander Specification

Commander は UPD Commander における「呼び出しの責任者」である。

## 1. 基本原則

1. Commander は実処理を実装してはならない。
2. Commander は、受け取った要求または返却結果に応じて、適切な Processing または Messenger を呼び出す。
3. Commander は層内の制御点として振る舞う。
4. Commander は層外の Processing を直接呼び出してはならない。
5. Commander 自身がデータ取得・計算・描画・整形を行ってはならない。

## 2. Commander が行ってよいこと

- コマンド種別の判別
- 呼び出す Processing の選択
- Processing へ必要な引数を渡す
- Processing の結果を受け取る
- 次に呼ぶ Processing を決定する
- 別層への要求が必要な場合に Messenger を呼び出す
- 別層から返ってきた結果を適切な Processing へ渡す
- 成功・失敗・継続など処理フロー上の分岐を行う

## 3. Commander が行ってはならないこと

以下は Commander に書いてはならない。

- `damage = attack - defense` のような計算
- HP 比率の計算
- JSON の整形
- ファイル読み込み
- SQL 実行
- pygame 描画
- 文字列装飾
- ゲームルール判定
- 保存用データ構造の生成

これらは各層の Processing に置く。

## 4. Commander の分岐

Commander は処理内容そのものではなく「どの処理を呼ぶか」を決める分岐を持ってよい。

許可例:

```text
command = attack
  -> Attack Processing を呼ぶ

command = save
  -> Save Request を Messenger に渡す
```

禁止例:

```text
if attack > defense:
    damage = attack - defense
else:
    damage = 1
```

後者はゲームロジックであり、Process Processing の責務である。

## 5. Processing が追加データを必要とする場合

Processing は別層へ直接要求を送らない。

推奨フロー:

```text
Processing
  -> 「追加データが必要」という結果を Commander に返す
Commander
  -> Messenger にデータ要求を渡す
Messenger
  -> 対象層へ送信
```

返却時:

```text
Messenger
  -> Commander
Commander
  -> 継続対象の Processing
```

## 6. Commander の肥大化判定

Commander 内に以下が増えた場合、責務違反を疑う。

- 計算式
- 複雑なループ
- データ変換
- UI フレームワーク呼び出し
- ファイル / DB API 呼び出し
- 業務ルールを表す条件式

Commander のコード量が増えたこと自体を問題とするのではなく、実処理が流入したことを問題とする。

## 7. Commander の状態保持

呼び出し継続に必要な最小限の状態保持は許可する。

例:

- どの要求への返却かを識別する ID
- 継続先 Processing の識別情報
- 非同期応答待ちの管理情報

ただし、ゲーム状態や表示状態そのものを Commander の責務として保持してはならない。
