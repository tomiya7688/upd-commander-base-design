# 15分で読む最小UPDチュートリアル

対象読者は、Pythonの関数とimportを知っていて、UPDを初めて使う人です。他の仕様書を先に読む必要はありません。短いメモを入力し、内容を検証してファイルへ保存し、結果を表示する機能を作ります。最後にCheckerで配置を確認します。

用語の意味は[日本語用語集](glossary.md)で確認できます。目安は説明を読む時間が15分程度です。Pythonの準備やファイルの作成時間は別に見込んでください。

## 1. 何をどこへ置くか（2分）

UPDは仕事を[UI](glossary.md#ui)・[Process](glossary.md#process)・[Data](glossary.md#data)の3つの[層](glossary.md#layer)に分けます。この例では、端末も利用者とのやり取りを担うUIです。

| 層      | この例で担当する仕事               | 担当しない仕事                           |
| ------- | ---------------------------------- | ---------------------------------------- |
| UI      | 入力を読む、保存結果を表示する     | メモの有効性を判断する、ファイルを書く   |
| Process | 空白を取り除き、空のメモを拒否する | 端末へ表示する、ファイルを書く           |
| Data    | メモをUTF-8のファイルへ書く        | 空のメモを許すか判断する、端末へ表示する |

各層の中でも、[Commander](glossary.md#commander)は「次に呼ぶ処理と順序を選ぶ」、[Messenger](glossary.md#messenger)は「隣り合う層へ要求や結果を運ぶ」、[Processing](glossary.md#processing)は「その層の実作業を行う」という[役割](glossary.md#role)を分担します。

守るのは仕事と通信の境界です。以下のファイル分割は読みやすくするための例であり、「必ず9クラスを作る」という規則ではありません。今回はクラスを使いません。

## 2. 作業フォルダを用意する（2分）

Python 3.10以上と、このリポジトリのローカルコピーを使います。以下はPowerShellで、リポジトリのルートを作業場所にした手順です。`python --version`が3.10以上を表示することを確認します。

```powershell
python --version
python -m pip install -e support_tools/python/upd_commander_checker
New-Item -ItemType Directory -Path tutorial-note
Set-Location tutorial-note
New-Item -ItemType Directory -Path ui, process, data
```

`tutorial-note`は新しい空のフォルダにしてください。既に同名のフォルダがある場合は別名にし、既存のファイルへ例を上書きしないでください。以後のファイル作成と実行は、このフォルダ内で行います。

エディタで次の11ファイルを作り、次章の内容をコピーします。各層のフォルダはPythonの名前空間パッケージとして使うため、この例では`__init__.py`は不要です。

```text
tutorial-note/
├─ main.py
├─ ui/
│  ├─ commander.py
│  ├─ messenger.py
│  └─ processing.py
├─ process/
│  ├─ commander.py
│  ├─ incoming_messenger.py
│  ├─ outgoing_messenger.py
│  └─ processing.py
└─ data/
   ├─ commander.py
   ├─ messenger.py
   └─ processing.py
```

ProcessのMessengerだけ入口と出口を別ファイルにしています。入口からCommanderを呼び、Commanderから出口を呼ぶ形にして、Pythonのimportが循環しないようにするためです。これは言語上の配置の工夫で、UPDの役割を増やすものではありません。

## 3. 小さな機能を作る（7分）

実作業、呼び分け、通信を順に置きます。各コード直前のファイル名は、`tutorial-note`からの相対パスです。

### 実作業をProcessingへ置く

UIは入力と表示、Processは入力の検証、Dataは保存だけを担当します。

`ui/processing.py`

```python
def read_note():
    return input("メモ: ")


def show_saved(text):
    print(f"保存しました: {text}")


def show_rejected():
    print("空のメモは保存できません。")
```

`process/processing.py`

```python
def validate_note(text):
    normalized = text.strip()
    return normalized if normalized else None
```

`None`は「保存できる内容がない」という検証結果です。保存するかどうかの判断は、UIの見た目や保存先の都合ではなく、Processの規則として決めます。

`data/processing.py`

```python
from pathlib import Path


def write_note(text):
    Path("note.txt").write_text(text + "\n", encoding="utf-8")
    return text
```

保存先は実行時の作業フォルダ内の`note.txt`です。成功するたびに以前の内容を上書きします。この教材専用のフォルダで実行し、既存の大切な`note.txt`を置かないでください。

### Commanderに呼ぶ順序を置く

同じ層のProcessingとMessengerだけを呼びます。文字列の加工、入出力、表示文の組み立てはCommanderへ入れません。

`ui/commander.py`

```python
from ui import messenger, processing


def run():
    text = processing.read_note()
    saved = messenger.save_note(text)
    if saved is None:
        processing.show_rejected()
    else:
        processing.show_saved(saved)
```

`process/commander.py`

```python
from process import outgoing_messenger, processing


def save_note(text):
    validated = processing.validate_note(text)
    if validated is None:
        return None
    return outgoing_messenger.save_note(validated)
```

この`if`はProcessingが返した結果に応じて次の呼び出しを選ぶだけです。空白の除去や「何を空と扱うか」という実処理はProcessing側にあります。

`data/commander.py`

```python
from data import processing


def save_note(text):
    return processing.write_note(text)
```

### Messengerを隣り合う層へつなぐ

受信側のMessengerは同じ層のCommanderへ渡します。MessengerからProcessingを直接呼びません。

`ui/messenger.py`

```python
from process import incoming_messenger


def save_note(text):
    return incoming_messenger.save_note(text)
```

`process/incoming_messenger.py`

```python
from process import commander


def save_note(text):
    return commander.save_note(text)
```

`process/outgoing_messenger.py`

```python
from data import messenger


def save_note(text):
    return messenger.save_note(text)
```

`data/messenger.py`

```python
from data import commander


def save_note(text):
    return commander.save_note(text)
```

最後に、プログラムの入口からUIのCommanderを起動します。

`main.py`

```python
from ui.commander import run


if __name__ == "__main__":
    run()
```

### 要求と結果が通る道を確かめる

下図の`→`は同期関数の呼び出しです。層を越えるのはMessenger同士の区間だけです。

```text
main → UI Commander → UI Processing（入力を読む）
         ↓ 入力が返る
       UI Messenger → Process入口Messenger → Process Commander
                                                 ↓
                                          Process Processing（検証）
                                                 ↓ 有効な内容が返る
                                          Process出口Messenger
                                                 ↓
                                          Data Messenger → Data Commander
                                                                 ↓
                                                          Data Processing（保存）
```

保存した文字列はPythonの`return`で呼び出し元へ逆順に戻ります。Data Processing → Data Commander → Data Messenger → Process出口Messenger → Process Commander → Process入口Messenger → UI Messenger → UI Commanderの順に戻り、UI CommanderがUI Processingへ表示を依頼します。別の層のProcessingへ直接戻したり、直接呼び出したりはしません。空の入力ならProcess Commanderから`None`が戻り、Dataへは要求を送りません。

この同期例では、関数の引数が要求、戻り値が応答の[メッセージ](glossary.md#message)に相当します。Messengerはキューや非同期通信を必須にする名前ではありません。受け渡すものが標準の文字列と`None`だけなので、独自の共通型を置く[Common](glossary.md#common)フォルダも不要です。Commonは第四層ではなく、必要な共有資産がある場合に使います。

## 4. 動かしてCheckerを1回実行する（4分）

まだ`tutorial-note`内にいることを確認して実行します。

```powershell
python main.py
```

たとえば`牛乳を買う`と入力すると、次のように表示され、同じ内容が`note.txt`へ保存されます。

```text
メモ: 牛乳を買う
保存しました: 牛乳を買う
```

前後の空白は取り除かれます。もう一度実行して空白だけを入力すると「空のメモは保存できません。」と表示され、以前の`note.txt`は変更されません。保存先への書き込み権限がない場合などは、この最小例ではPythonの例外で停止します。実用時の失敗の扱いは[Error Handling](../specification/error-handling.md)で確認してください。

次に、この11ファイルを対象に[Checker](glossary.md#checker)を実行します。インストールしたPythonと同じ`python`を使います。

```powershell
python -m upd_commander_checker .
```

上の例のままなら出力は`OK`、終了コードは0です。`OK`は静的に検出する[規則](glossary.md#rule)に該当しなかったという意味で、実行時の正しさや全設計の保証ではありません。問題があると規則番号・ファイル・行を含む[Finding](glossary.md#finding)が出ます。詳細は[Python Checker README](../support_tools/python/upd_commander_checker/README.md)を参照してください。

## 次に進む前の確認

- 入力・表示はUI、入力の検証はProcess、保存はDataに置けましたか。
- Commanderは呼び分け、Messengerは隣接層への通信、Processingは実作業になっていますか。
- UIからDataへ飛び越えず、MessengerからProcessingを直接呼んでいませんか。
- 空の入力で保存先が変わらないことを確認できましたか。

この例では、処理と通信の境界を守ることが必須です。一方、このファイル分割やCommonを設けない選択は例の実装方法です。[Compresser](glossary.md#compresser)などの補助も、最初から導入する必須条件ではありません。必須・推奨・小技の区別は[Guideline Levels](../specification/guideline-levels.md)、別の機能で配置を考えるときは[Architecture](architecture.md)、経路を詳しく追うときは[Data Flow](data-flow.md)へ進んでください。
