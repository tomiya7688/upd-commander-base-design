# Testing Rules

本書は UPD Commander に従う実装のテスト規定を定める。

## 1. 基本原則

1. 各層は可能な限り独立してテスト可能でなければならない。
2. Processing は Commander や Messenger を起動しなくても単体テストできる構造を推奨する。
3. Commander は実処理の正しさではなく、正しい処理・Messenger を呼び出すことをテストする。
4. Messenger は業務結果ではなく、正しい宛先へ正しい契約で送受信できることをテストする。
5. 層間統合テストでは正式な通信経路を通ることを確認する。

## 2. UI Layer

UI Processing のうち、表示値への変換や表示状態判定は描画本体と分離してテスト可能にすることを推奨する。

例:

- HP 24/100 -> ratio 0.24
- ratio が閾値以下 -> warning=true

pygame 等を必要とする描画テストと、純粋な表示変換テストを分けてよい。

## 3. Process Layer

Process Processing は最も重点的に単体テストする。

テスト対象例:

- 入力値に対する計算結果
- 状態遷移
- 境界値
- 無効入力
- 追加データ要求の返却

UI や Data の実装を起動せずテストできる構造を推奨する。

## 4. Data Layer

Data Processing は読み書き形式と変換をテストする。

外部 DB や実ファイルを使用するテストと、メモリ上・一時領域・モックを用いるテストを分離してよい。

## 5. Commander テスト

Commander のテストでは以下を確認する。

- command A で Processing A を呼ぶ
- Data が必要なら Messenger を呼ぶ
- Data 返却後に正しい Processing を再開する
- Processing 結果を正しい Messenger へ渡す
- Commander 自身で値を加工していない

## 6. Messenger テスト

Messenger のテストでは以下を確認する。

- 正しい宛先へ送る
- message contract を維持する
- request / response を対応づけられる
- 通信失敗を検出できる

Messenger のテストでゲームロジックの正しさを検証してはならない。

## 7. アーキテクチャテスト

可能な言語・環境では静的解析やテストにより禁止依存を検出することを推奨する。

検出対象例:

- UI から Data の import
- Processing から別層 Processing の import
- Messenger から Processing の直接呼び出し
- Process に pygame 依存が混入
- UI に DB driver 依存が混入

## 8. 回帰テスト

責務移動やリファクタリング後も、外部から見た command / response の契約を維持する。

契約変更が必要な場合は message contract の変更として明示的に扱う。
