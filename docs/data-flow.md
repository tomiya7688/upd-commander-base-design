# Data Flow

**日本語** | [English](data-flow.en.md)

この文書は、UI・Process・Dataの間で要求と結果がどう渡るかを説明します。図は責務の移動を示す概念図であり、特定の関数呼出し順や同期方式を指定するものではありません。

## 図の記号

| 表記 | 意味 |
| --- | --- |
| `--[呼出]-->` | Commanderが同じ層のProcessingまたはMessengerへ処理を依頼する |
| `--[結果返却: ...]-->` | 呼び出されたProcessingが結果をCommanderへ返す。追加データ要求も結果として返す |
| `==[メッセージ: ...]==>` | 層間のMessengerが隣接層のMessengerへ要求または結果を運ぶ |
| `--[受渡し]-->` | 受信したMessengerが同じ層のCommanderへ内容を渡す |

`結果返却`は処理結果の受渡しを表し、呼出し元が返答を待つ同期処理を必須としません。Messengerの通信は、返答を待つ同期処理・返答を後から受け取る非同期処理のどちらでも実装できます。複数の非同期要求を対応付ける方法は[Message Contract](../specification/message-contract.md)を参照してください。

`メッセージ`は通信責務の境界を示し、メッセージキューや非同期処理を必須にしません。Messengerの内部で直接メソッド呼出しを使う実装も可能です。

## 要求の流れ

UIから始まった要求にProcessが追加データを必要とする場合の経路です。

```text
UI Commander --[呼出]--> UI Processing
UI Processing --[結果返却: 入力値]--> UI Commander
UI Commander --[呼出]--> UI Messenger
UI Messenger ==[メッセージ: 要求]==> Process Messenger
Process Messenger --[受渡し]--> Process Commander
Process Commander --[呼出]--> Process Processing
Process Processing --[結果返却: 追加データ要求]--> Process Commander
Process Commander --[呼出]--> Process Messenger
Process Messenger ==[メッセージ: データ要求]==> Data Messenger
Data Messenger --[受渡し]--> Data Commander
Data Commander --[呼出]--> Data Processing
```

Process Processingが追加データを必要とするとき、Processingは「どのデータが必要か」を処理結果としてCommanderへ返します。Processing自身がMessengerを呼んだり、別層へ直接要求を送ったりするわけではありません。Commanderが返却結果を受けて、Messengerへ要求送信を依頼します。

## 応答の流れ

Dataから取得結果が返り、Processが処理を再開してUIへ最終結果を返す経路です。

```text
Data Processing --[結果返却: 取得結果]--> Data Commander
Data Commander --[呼出]--> Data Messenger
Data Messenger ==[メッセージ: 取得結果]==> Process Messenger
Process Messenger --[受渡し]--> Process Commander
Process Commander --[呼出: 処理を再開]--> Process Processing
Process Processing --[結果返却: 最終結果]--> Process Commander
Process Commander --[呼出]--> Process Messenger
Process Messenger ==[メッセージ: 最終結果]==> UI Messenger
UI Messenger --[受渡し]--> UI Commander
UI Commander --[呼出]--> UI Processing
```

UI Processingは最終結果を画面表示へ変換できます。反対に、Dataへ保存する場合も、Process CommanderがMessengerへ保存要求を渡し、Data側が処理します。いずれの経路でもUI MessengerとData Messengerは直接通信しません。

## 1つの機能を通した例: 商品検索

検索画面で入力したキーワードから商品を検索し、結果一覧を表示します。

1. UI Processingが入力欄からキーワードを読み、UI Commanderへ結果を返します。
2. UI CommanderがUI Messengerへ検索要求を渡し、UI MessengerがProcess Messengerへ要求メッセージを送ります。
3. Process Messengerは要求をProcess Commanderへ渡し、Process Commanderは検索条件を扱うProcess Processingを呼びます。
4. Process Processingが商品情報を必要と判断した場合、「商品検索データが必要」という結果をProcess Commanderへ返します。これは追加要求を表す結果であり、ProcessingからMessengerへの直接呼出しではありません。
5. Process CommanderはProcess Messengerへデータ要求を依頼します。Process MessengerはData Messengerへ要求メッセージを送り、Data Commanderが商品読込のData Processingを呼びます。
6. Data Processingが取得結果をData Commanderへ返すと、Data MessengerがProcess Messengerへ応答メッセージを送り、Process Messengerが結果をProcess Commanderへ渡します。Process CommanderはProcess Processingを再開します。
7. Process Processingが検索結果を返すと、Process CommanderはProcess Messengerへ最終結果を渡します。Process MessengerはUI Messengerへ結果メッセージを送り、UI Messengerは結果をUI Commanderへ渡します。UI CommanderがUI Processingを呼び、一覧を表示します。

この例の`検索要求`や`追加要求`は役割を説明するための概念名です。具体的な型、メッセージ名、同期・非同期の選択は実装に合わせて決めます。層と役割の全体像は[Architecture](architecture.md)、CommanderとMessengerの責務は[Commander & Messenger](commander-messenger.md)を参照してください。
