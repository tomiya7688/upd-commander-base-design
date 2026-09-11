# Error Handling Specification

本書は UPD Commander におけるエラーの扱いと伝播を規定する。

## 1. 基本原則

1. エラーは発生した層で検出する。
2. その層で解決可能なエラーは、その層の Processing で処理してよい。
3. 呼び出し元の判断が必要なエラーは Commander へ返す。
4. 層を越えて通知する必要がある場合は Messenger を経由する。
5. Messenger がエラーの意味を業務判断してはならない。

## 2. UI Layer のエラー

UI 固有のエラーは UI Layer で扱う。

例:

- 描画リソース不足
- UI 部品生成失敗
- 入力デバイス関連エラー

Process に通知する必要がない UI 表示上の失敗は UI 内で完結してよい。

## 3. Process Layer のエラー

ゲームルール・業務ロジック上の失敗は Process Layer で判断する。

例:

- 行動条件不足
- 対象が存在しない
- MP 不足
- 無効な状態遷移

UI に表示が必要な場合は、Process Layer が意味上の失敗情報を返し、UI Layer が表示形式を決める。

Process Layer が直接エラーメッセージの色やダイアログ形式を決めてはならない。

## 4. Data Layer のエラー

データ取得・保存に関する失敗は Data Layer で検出する。

例:

- ファイルが存在しない
- DB 接続失敗
- 形式不正
- 読み書き失敗

Data Layer は必要に応じて内部例外を層間契約用の失敗情報へ変換する。

## 5. 伝播

Data 由来のエラーを UI へ通知する場合でも、経路は変更しない。

```text
Data Processing
  -> Data Commander
  -> Data Messenger
  -> Process Messenger
  -> Process Commander
  -> Process Processing / Process判断
  -> Process Commander
  -> Process Messenger
  -> UI Messenger
  -> UI Commander
  -> UI Processing
```

Process Layer は、Data Layer の技術的失敗をゲーム・業務上どう扱うか判断してよい。

## 6. 例外の越境

言語固有の例外オブジェクトをそのまま別層へ漏らすことは避ける。

特に Data Layer の DB ドライバ例外や UI フレームワーク例外を他層へ直接渡さない。

必要に応じて以下のような共通失敗情報へ変換する。

```text
error:
  code: save_failed
  detail: optional detail
```

## 7. ログ

ログ出力はエラー処理そのものとは分離する。

各層は自身の技術情報をログへ出力してよいが、ログ出力を理由として責務境界を破ってはならない。
