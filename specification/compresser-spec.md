# Compresser Specification

Compresser は、Commander / Messenger 間で受け渡される入力値・返却値を、クラス単位の Container としてまとめ、通信単位として扱いやすくするための専用コンポーネントである。

## 1. 目的

UPD Commander では、Commander と Messenger は処理内容そのものではなく、処理の選択・呼び出し・配送を担当する。

しかし、呼び出しに多数の引数や返却値を直接並べると、Commander / Messenger のコード量が増え、通信内容の構造まで中継側が知ることになる。

Compresser はこの問題を避けるため、1つのクラスに渡す入力値を1つの Input Container にまとめ、返却値を1つの Output Container にまとめる。

基本思想は次の通りとする。

> 値の意味を知る必要があるのは、原則として通信の両端だけである。中継する Commander / Messenger は Container の中身を理解せず、通信単位として運ぶ。

## 2. 基本原則

1. 1つのクラスに渡される入力引数群は、原則として1つの Input Container にまとめる。
2. 1つのクラスから返される返却情報は、原則として1つの Output Container にまとめる。
3. 戻り値が単一値である場合も、その値自体が実質的に1つの返却単位であるため、無理に複雑な Container を作る必要はない。ただし複数の返却情報を持つ場合は1つの Output Container にまとめる。
4. Compresser 自体の実装単位は固定しない。1つのクラス専用に1つの Compresser を定義してもよく、関連する複数クラスを機能単位でまとめて1つの Compresser が扱ってもよい。
5. Compresser が複数クラスを扱う場合でも、1クラス1 Container の境界を壊してはならない。
6. 通信時に複数の Container を1つの Package / Message にまとめて送信してよい。
7. 異なるクラスや異なる責務の Container を、単一の巨大な共通 Container に無理に統合してはならない。
8. Commander / Messenger は、原則として Compresser が生成した Container / Package の内部構造を業務的に解釈してはならない。
9. Container の生成・格納・取り出し方法に関する責務は Compresser が持つ。
10. Compresser は値の格納形式を扱うが、業務処理・表示判断・保存判断を行ってはならない。
11. 通信単位は UI フレームワーク、DB 接続、ファイルハンドル等の層固有実装を漏らしてはならない。
12. Compresser の導入によって Messenger / Commander に Processing の責務を移してはならない。

## 3. クラス単位の Container

基本形は次の通りとする。

```text
ClassA
  Input  -> ClassAInputContainer
  Output -> ClassAOutputContainer
```

多数の値を直接渡す形:

```text
ClassA(
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
input = ClassAInputContainer(...)
ClassA(input)
```

返却側も、複数の返却情報を持つ場合は次のようにまとめる。

```text
return ClassAOutputContainer(
  result,
  status,
  error,
  metadata
)
```

これにより、クラスの呼び出しシグネチャは原則として入力1つ・出力1つに保つ。

## 4. Compresser の実装粒度

Compresser の数と配置は、プロジェクトの規模や機能境界に応じて選択してよい。

### クラス単位

1つのクラス専用の Compresser を用意する形。

```text
ClassACompresser
  -> ClassAInputContainer
  -> ClassAOutputContainer

ClassBCompresser
  -> ClassBInputContainer
  -> ClassBOutputContainer
```

### 機能単位

関連する複数クラスの Container を、1つの機能用 Compresser がまとめて扱う形。

```text
UserFeatureCompresser
  -> CreateUserInputContainer
  -> CreateUserOutputContainer
  -> UpdateUserInputContainer
  -> UpdateUserOutputContainer
```

どちらの方式を採用してもよい。

ただし、Compresser の実装粒度と Container の責務境界は別である。

```text
Compresser = 複数クラス分をまとめてもよい
Container  = 1クラスごとに分離する
```

機能単位の Compresser を採用したことを理由に、複数クラスの入力や返却値を1つの共通 Container に統合してはならない。

## 5. 複数 Container の送信

1通信につき1 Container である必要はない。

複数のクラス向け Container をまとめて送信してよい。

```text
MessagePackage
  ClassAInputContainer
  ClassBInputContainer
  ClassCInputContainer
```

受信側は Package 全体を受け取り、必要な Container を取り出して使用する。

重要なのは、次の2つを区別することである。

```text
1クラス = 原則1 Input Container + 1 Output Container
1通信   = 1個以上の Container をまとめてよい
```

複数 Container をまとめるために、それぞれの責務を壊して1つの巨大な Container に統合してはならない。

悪い例:

```text
CommonContainer
  user_name
  hp
  position
  file_path
  save_data
  ui_state
```

良い例:

```text
MessagePackage
  UserContainer
  CharacterContainer
  SaveContainer
```

## 6. 受信側での扱い

受信側も Container / Package の状態で受け取る。

受信直後にすべての値をばらして多数のローカル引数へ戻すことを前提とはしない。

Processing 等、値の意味を扱う責務を持つ側は、必要な Container やフィールドを必要な時点で確認しながら処理してよい。

概念例:

```text
package = receive()

input = package.get(ClassAInputContainer)
mode = input.mode

if mode == X:
  value = input.value_x

if mode == Y:
  value = input.value_y
```

このとき Messenger は `mode` や `value_x` の意味を判断してはならない。

## 7. 責務

Compresser が担当してよいもの:

- 複数入力値の Input Container への格納
- 複数返却値の Output Container への格納
- Container からの値取得を可能にする構造の提供
- 複数 Container の Package / Message への格納
- 通信契約に従ったフィールド配置
- request / response 用 Container の生成
- 言語や通信方式に応じた非業務的なシリアライズ準備

Compresser が担当してはならないもの:

- ダメージ計算
- 入力値の業務ルール判定
- UI 表示内容の決定
- DB クエリ生成
- セーブデータの意味上の変換
- Commander の代わりに処理順序を決定すること
- Messenger の代わりに宛先を決定すること

## 8. Commander / Messenger との関係

責務は次のように分離する。

```text
Commander
  何を呼び出すかを決める

Messenger
  どこへ運ぶかを担当する

Compresser
  クラス単位の入力・出力を Container としてまとめる
  必要に応じて複数 Container を Package としてまとめる
  実装自体はクラス単位または機能単位で配置してよい

Processing
  Container 内の値の意味を使って実際の処理を行う
```

Commander / Messenger は通信単位を転送するために必要な `command`、`request_id`、宛先情報等を扱ってよい。

ただし、Container 内部の個々の値を読み、その意味に基づいて業務判断することは原則として禁止する。

## 9. Request / Response の分離

要求と返却で構造が異なる場合は、入力用・出力用 Container を分離する。

例:

```text
CreateUserInputContainer
CreateUserOutputContainer
```

Compresser 自体については、クラス単位でも機能単位でもよい。

ただし、単一の Compresser が関連性のない多数の機能を無制限に扱い、Compresser 自体が責務過多になることは避ける。

## 10. 通信方式からの独立

Compresser が作る Container / Package は、可能な限り通信手段に依存しないものとする。

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
Container / Package を送る
Container / Package を受け取る
```

## 11. Message Contract との関係

Compresser は `message-contract.md` に定義された契約に従って Container / Package を構築する。

Compresser 自体が契約を独自に拡張してはならない。

契約変更が必要な場合は Message Contract を更新し、その契約に従って各 Compresser を変更する。
