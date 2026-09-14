# Compresser Specification

Compresser は、Commander / Messenger 間で受け渡される複数の入力値・返却値を、単一の通信単位へまとめ、必要な地点で展開するための専用コンポーネントである。

## 1. 目的

UPD Commander では、Commander と Messenger は処理内容そのものではなく、処理の選択・呼び出し・配送を担当する。

しかし、呼び出しに多数の引数や返却値を直接並べると、Commander / Messenger のコード量が増え、通信内容の構造まで中継側が知ることになる。

Compresser はこの問題を避けるため、複数の値を1つの Package / Message / DTO 等へまとめて受け渡せるようにする。

基本思想は次の通りとする。

> 値の意味を知る必要があるのは、原則として通信の両端だけである。中継する Commander / Messenger は箱の中身を理解せず、1つの通信単位として運ぶ。

## 2. 基本原則

1. 複数の入力値・返却値は、必要に応じて Compresser によって単一の通信単位へまとめてよい。
2. Commander / Messenger は、原則として Compresser が生成した通信単位の内部構造を解釈してはならない。
3. パッケージ化および展開の責務は Compresser が持つ。
4. Compresser は値の格納形式を扱うが、業務処理・表示判断・保存判断を行ってはならない。
5. 通信単位は UI フレームワーク、DB 接続、ファイルハンドル等の層固有実装を漏らしてはならない。
6. Compresser の導入によって Messenger / Commander に Processing の責務を移してはならない。

## 3. 基本形

多数の値を直接渡す形:

```text
Commander -> Messenger(
  user_id,
  user_name,
  age,
  address,
  token,
  option,
  retry_count
)
```

Compresser を利用する形:

```text
package = RequestCompresser.compress(...)
Commander -> Messenger(package)
```

返却も同様に扱う。

```text
package = Messenger.receive()
result = ResponseCompresser.decompress(package)
```

Commander / Messenger にとって重要なのは、原則として `package` を受け取り、適切な宛先へ渡すことであり、その内部フィールドの業務的意味ではない。

## 4. 責務

Compresser が担当してよいもの:

- 複数値の単一 DTO / record / struct / message への格納
- DTO / record / struct / message からの値の展開
- 通信契約に従ったフィールド配置
- request / response 用通信単位の生成
- 言語や通信方式に応じた非業務的なシリアライズ準備

Compresser が担当してはならないもの:

- ダメージ計算
- 入力値の業務ルール判定
- UI 表示内容の決定
- DB クエリ生成
- セーブデータの意味上の変換
- Commander の代わりに処理順序を決定すること
- Messenger の代わりに宛先を決定すること

## 5. Commander / Messenger との関係

責務は次のように分離する。

```text
Commander
  何を呼び出すかを決める

Messenger
  どこへ運ぶかを担当する

Compresser
  何を1つの通信単位としてまとめ、どう展開するかを担当する

Processing
  値の意味を使って実際の処理を行う
```

Commander / Messenger は通信単位を転送するために必要な `command`、`request_id`、宛先情報等を扱ってよい。

ただし、payload 内部の個々の値を読み、その意味に基づいて業務判断することは原則として禁止する。

## 6. Request / Response の分離

要求と返却で構造が異なる場合は、責務を明確にするため Compresser を分離することを推奨する。

例:

```text
CreateUserRequestCompresser
CreateUserResponseCompresser
```

単一の Compresser が複数の無関係な通信形式を扱い、責務過多になることは避ける。

## 7. 通信方式からの独立

Compresser が作る通信単位は、可能な限り通信手段に依存しないものとする。

そのため、以下のような通信方式へ変更しても Commander / Processing 側への影響を小さく保てる設計を推奨する。

- 直接メソッド呼び出し
- イベント / メッセージキュー
- IPC
- Socket
- HTTP
- async / await
- 別プロセス
- 別言語間通信

中継側から見た基本形は、通信方式に関係なく次の形を維持することを目標とする。

```text
1つ送る
1つ受け取る
```

## 8. Message Contract との関係

Compresser は `message-contract.md` に定義された契約に従って通信単位を構築する。

Compresser 自体が契約を独自に拡張してはならない。

契約変更が必要な場合は Message Contract を更新し、その契約に従って各 Compresser を変更する。
