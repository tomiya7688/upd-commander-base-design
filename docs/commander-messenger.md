# Commander & Messenger

用語の意味は[日本語用語集](glossary.md)で確認できます。

## Commander

[Commander](glossary.md#commander) は[各層](glossary.md#layer)における呼び出しの交通整理を担当します。

### Commander が行うこと

- [Messenger](glossary.md#messenger) から届いた要求を受け取る
- 適切な [Processing](glossary.md#processing) を呼ぶ
- 必要に応じて Messenger に他層への要求送信を依頼する
- Processing の結果を適切な次の処理へ渡す

### Commander が行わないこと

- ダメージ計算
- 表示用文字列の生成
- ファイル読み書き
- データ変換
- 複雑な条件判定

Commander にこれらが増えた場合、Processing へ移動します。

## Messenger

Messenger は層間通信を担当します。

### Messenger が行うこと

- 隣接層へコマンドまたはデータを送る
- 隣接層から届いたコマンドまたはデータを受け取る
- 受信内容を自層の Commander に渡す

### Messenger が行わないこと

- ゲームロジックの判断
- [UI](glossary.md#ui)表示判断
- データ整形
- 保存処理
- 呼び出し先 Processing の選定

## 呼び出し方向

典型的な要求は次のように流れます。

```text
UI Commander
  ↓
UI Messenger
  ↓
Process Messenger
  ↓
Process Commander
  ↓
Process Processing
```

データが必要な場合:

```text
Process Commander
  ↓
Process Messenger
  ↓
Data Messenger
  ↓
Data Commander
  ↓
Data Processing
```

返却時は逆方向に Messenger を通ります。

## Commander と Processing の境界

判断基準は単純です。

> 「どの処理を呼ぶか」は Commander。
> 「その処理をどう実行するか」は Processing。

この[境界](glossary.md#boundary)を守ることで Commander を薄く保ちます。
