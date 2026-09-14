# Message Contract

本書は Messenger 間で受け渡すメッセージの最低限の契約を規定する。

## 1. 基本原則

1. メッセージは送信元層の内部実装を受信先へ漏らしてはならない。
2. メッセージは受信先が処理判断に必要な情報を明示的に含む。
3. UI フレームワーク、DB 接続、ファイルハンドル等の実装依存オブジェクトを層間メッセージとして渡してはならない。
4. メッセージ形式は言語や通信手段に依存しない概念として定義する。
5. 複数の入力値・返却値は、必要に応じて Compresser により単一の通信単位へまとめてよい。
6. Commander / Messenger は、原則として payload 内部の個々の値の業務的意味を解釈しない。

## 2. 推奨フィールド

必要に応じて次の情報を持たせる。

```text
command       : 実行要求の種別
request_id    : 要求識別子
payload       : 要求または返却値
status        : success / failure / pending 等
action        : 次に必要な意味上の操作
error         : 失敗時情報
```

すべてを常に必須とはしない。

複数値を扱う場合でも、Messenger / Commander の引数として個別に並べるのではなく、可能な限り `payload` 等の単一の通信単位へまとめる。

## 3. Command

`command` は「何を要求しているか」を表す。

例:

```text
move_player
attack
load_character
save_game
```

Messenger 自身は command の業務的意味を処理してはならない。

## 4. Payload

payload は層間で必要な最小限の情報とする。

良い例:

```text
{
  character_id: "abc123"
}
```

悪い例:

```text
{
  pygame_event: <pygame.Event>,
  db_connection: <Connection>
}
```

payload の生成・展開を専用化する場合は `compresser-spec.md` に従う。

Commander / Messenger は配送に必要な範囲を除き、payload の内部フィールドを読み、その意味に応じた判断を行ってはならない。

## 5. Request / Response 対応

非同期処理や複数要求が並行する場合は `request_id` 等で要求と返却を対応づける。

名称や具体形式は実装側で変更可能だが、どの返却がどの要求に対応するか識別できなければならない。

## 6. Processing からの追加要求

Processing が処理継続のために別層データを必要とする場合、Processing の結果として「必要な操作」を Commander に返してよい。

概念例:

```text
status: pending
action: request_data
payload:
  command: load_character
  character_id: abc123
```

Commander はこれを解釈し Messenger を呼ぶ。

Processing が直接 Messenger を呼ぶための抜け道として message contract を使用してはならない。

## 7. Response

返却メッセージは必要に応じて成功 / 失敗を明示する。

成功例:

```text
status: success
request_id: 123
payload:
  hp: 100
  attack: 20
```

失敗例:

```text
status: failure
request_id: 123
error:
  code: data_not_found
  detail: character data not found
```

複数の返却値を直接返す代わりに、Compresser によって1つの response package として構築してよい。

## 8. 型の共有

DTO、構造体、record 等を用いて契約を型として表現することを推奨する。

ただし、その型定義に UI / Process / Data の実処理を持たせてはならない。
