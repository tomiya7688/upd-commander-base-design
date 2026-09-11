# Dependency Rules

本書は UPD Commander における依存関係の許可・禁止を規定する。

## 1. 基本依存

許可される層間境界は次の2つだけである。

```text
UI <-> Process
Process <-> Data
```

`UI <-> Data` の直接依存は禁止する。

## 2. 許可表

| 呼び出し元 | 呼び出し先 | 許可 | 条件 |
|---|---|---:|---|
| UI Commander | UI Processing | Yes | 同一層の処理呼び出し |
| UI Commander | UI Messenger | Yes | Process への要求送信 |
| UI Processing | UI Commander | Yes | 処理結果・追加要求の返却 |
| UI Messenger | Process Messenger | Yes | 層間通信 |
| Process Messenger | Process Commander | Yes | 受信要求の引き渡し |
| Process Commander | Process Processing | Yes | 同一層の処理呼び出し |
| Process Commander | Process Messenger | Yes | UI/Data への送信 |
| Process Processing | Process Commander | Yes | 処理結果・追加要求の返却 |
| Process Messenger | Data Messenger | Yes | Data への層間通信 |
| Data Messenger | Data Commander | Yes | 受信要求の引き渡し |
| Data Commander | Data Processing | Yes | 同一層の処理呼び出し |
| Data Commander | Data Messenger | Yes | Process への返却 |
| Data Processing | Data Commander | Yes | 処理結果の返却 |

## 3. 禁止表

| 呼び出し元 | 呼び出し先 | 理由 |
|---|---|---|
| UI Layer | Data Layer | Process を飛び越えるため |
| UI Processing | Process Processing | Messenger/Commander を迂回するため |
| UI Processing | Data Processing | 2層を飛び越えるため |
| Process Processing | Data Processing | Commander/Messenger を迂回するため |
| Data Processing | Process Processing | 逆方向にゲームロジックへ侵入するため |
| Data Layer | UI Layer | 表示責務を Data が知ることになるため |
| Messenger | 任意の Processing | Commander を迂回するため |
| 別層 Commander | 別層 Processing | Messenger を迂回するため |

## 4. Import / Reference 規定

実装言語に import / include / using 等が存在する場合、依存規則はソースコード上の参照にも適用する。

例えば UI Layer から Data Layer のクラスを import することは、実際に呼び出していなくても原則禁止する。

共通 DTO、Message Contract、値オブジェクト等を共有する必要がある場合は、特定層の実装に属さない共通契約領域へ置くことを許可する。

ただし共通領域へゲーム処理、描画処理、データ処理を置いて規則を回避してはならない。

## 5. 外部ライブラリ依存

外部ライブラリは、それを必要とする層へ閉じ込める。

例:

- pygame -> UI
- DB driver -> Data
- ゲームロジック用純粋計算ライブラリ -> Process

外部ライブラリの型を境界越しに漏らさないことを推奨する。

## 6. 例外

規定外の直接依存が必要な場合は、実装上の都合だけを理由に例外化してはならない。

例外を設ける場合は、少なくとも以下を文書化する。

- なぜ通常経路で実装できないか
- 依存範囲
- 代替案
- 将来除去可能か

一時的な例外は技術的負債として追跡する。
