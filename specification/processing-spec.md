# Processing Specification

用語の意味は[日本語用語集](../docs/glossary.md)で確認できます。

[Processing](../docs/glossary.md#processing) は[各層](../docs/glossary.md#layer)における実処理を担当する。

## 1. 共通原則

1. 実際の計算・変換・描画・データ操作は Processing に置く。
2. Processing は自身の層の[責務](../docs/glossary.md#responsibility)範囲だけを扱う。
3. Processing は別層の Processing を直接呼び出してはならない。
4. 別層の情報が必要な場合は [Commander](../docs/glossary.md#commander) に要求を返す。
5. Processing は可能な限り単一責務で分割する。

## 2. UI Processing

[UI](../docs/glossary.md#ui) Processing は表示・入力解釈・UI 固有変換を担当する。

担当例:

- pygame 描画
- 表示文字列生成
- HP 値から表示ゲージ幅への変換
- UI 上の警告表示判定
- 画面遷移演出
- 入力値の UI 上の解釈

UI Processing はゲームルールや保存処理を持たない。

## 3. Process Processing

[Process](../docs/glossary.md#process) Processing はアプリケーション本体の規則・計算を担当する。

担当例:

- 戦闘計算
- AI 判断
- ステータス更新
- 勝敗判定
- 経験値計算
- ゲームルール上の状態遷移

Process Processing は UI 固有表現や保存方式を知らない。

## 4. Data Processing

[Data](../docs/glossary.md#data) Processing はデータ取得・保存およびそれに必要な変換を担当する。

担当例:

- JSON / CSV / DB / バイナリの読み書き
- シリアライズ / デシリアライズ
- 外部形式から内部受け渡し形式への整形
- 保存用形式への変換

Data Processing はデータのゲーム上の意味や表示方法を判断しない。

## 5. Processing の返却

Processing は結果を Commander に返す。

返却結果は以下のような意味を持ってよい。

- 完了結果
- 次の Processing に渡す値
- 別層データが必要であることを示す要求
- 失敗情報

Processing 自身が [Messenger](../docs/glossary.md#messenger) を使って別層へ送信することは原則禁止する。

## 6. 単一責務

一つの Processing が複数の独立した処理を持つ場合は分割する。

例:

```text
BattleProcessing
```

に全戦闘処理を集約するのではなく、必要に応じて以下のように分割する。

```text
DamageProcessing
StatusEffectProcessing
TurnOrderProcessing
VictoryProcessing
```

分割単位は言語の class / [module](../docs/glossary.md#module) / function に固定しないが、責務[境界](../docs/glossary.md#boundary)が追跡可能でなければならない。
