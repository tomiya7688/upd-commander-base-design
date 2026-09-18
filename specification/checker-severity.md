# Checker Severity Boundary

この文書は、UPD Commander Checker / Tester の severity 分類の正本です。

設計上の規則の強さは [Guideline Levels](guideline-levels.md) の「必須 / 推奨 / 小技」に従います。
Checker の severity は、その分類を機械的に対応させたものです。

```text
必須（UPD Core Rules）     → Error
推奨（Recommended Structure） → Warning
小技（Practical Tips）       → Attention
```

実装都合や検出のしやすさだけで severity を決めてはいけません。
severity の意味は設計分類に従い、4言語で同一に保ちます。

## 1. Error = 必須違反

UPD として成立するための最低条件を破るものです。

原則として通常の CI でも失敗条件とします。

### 判定基準

- UI / Process / Data の責務分離を破る
- UI から Data へ直接依存する
- 別層 Processing を直接呼び出す
- Commander が独立した業務処理・ゲーム処理・保存処理を抱え込み、交通整理の役割を失う
- Messenger / Communicator が通信以外の独立した実処理を持つ
- 層固有型を不必要に境界越しへ漏らし、依存経路を曖昧にする

### 代表ルール（現状）

| Rule | 概要 |
|------|------|
| UPD101 | 層を越える不正な依存（例: UI → Data） |
| UPD102 | Application 境界を破る依存 |
| UPD103 | Data Commander 間の不正な直接通信 等 |
| UPD201 | Commander が実処理を持つ |
| UPD202 | Commander 内の禁止された計算・ループ等 |
| UPD203 | Commander / Messenger からの外部 I/O・API 直接呼び出し |

（各 rule の詳細定義は dependency-rules / commander-spec / data-commander-communication 等を正とする）

Error は「推奨を守っていない」ことを理由に出してはいけません。

## 2. Warning = 推奨違反

UPD としては成立しているが、保守性・探索性・可読性・責務分離の質が低下している状態です。

性能・メモリ・呼び出しコスト・言語仕様・既存資産などの事情で意図的に許容して構いません。

一般プロジェクトでは既定で CI 失敗にしません。
`warnings_as_errors` 等の厳格設定で利用者が昇格できます。

### 判定基準

- 責務単位（ファイル / モジュール / クラス）が過大
- 1ファイルに複数の主要責務型がある
- Namespace / package / module とフォルダ構成が不必要に乖離している
- Processing をより細かく閉じられる余地がある
- Application / Sub Application 分割で見通しが良くなる
- Compresser / Container 導入により Commander / Messenger を大幅に圧縮できる

### 代表ルール（現状）

| Rule | 概要 |
|------|------|
| UPD401 | 責務単位が過大（250行超 or 13メソッド超） |
| UPD402 | 1ファイルに複数の主要責務型 |
| UPD404 | 同居するデータ専用型が外部参照されている |
| UPD303 | Compresser/Container 導入で Commander/Messenger を大幅圧縮できる |

## 3. Attention = 小技 / 軽微な改善候補

UPD 適合性にも設計品質の主要部分にも直結せず、より読みやすく・扱いやすくするための改善候補です。

既定では CI 失敗条件にしません。
`attentions_as_errors` で昇格可能です。

### 判定基準

- コメントの付け方・命名・配置の補助的ヒント
- 軽微な Container 化候補（複数入力 / 複数返却値）
- データ専用型の同居（外部参照なし）
- レビュー時の探索性改善
- 導入時の便利な構成例

### 代表ルール（現状）

| Rule | 概要 |
|------|------|
| UPD301 | 有効入力数が設定上限を超過 → Input Container 検討 |
| UPD302 | 複数返却値 → Output Container 検討 |
| UPD403 | データ専用型が別の型と同居（外部参照なし） |
| UPD405 | 大規模な UI / Process / Data で大部分のソースが layer root 直下に集中 |

## 4. Self Check と一般利用の分離

| モード | Error | Warning | Attention |
|--------|--------|---------|-----------|
| 一般プロジェクト既定 | CI失敗 | 失敗にしない | 失敗にしない |
| Checker 自身の strict Self Check | 失敗 | 失敗 | 失敗 |

Checker 自身は設計例として扱うため、Self Check では Warning / Attention も残さないことを目標とします。
これは一般プロジェクトへ推奨・小技を強制する規定ではありません。

## 5. 変更時の同期規則

- severity の意味を変える場合は、先にこの文書を更新する
- 既存 rule の severity を上げ下げする場合は、対応する rule 仕様と 4言語実装・回帰テストを同一変更で同期する
- 推奨事項を Error に昇格させない
- 小技を Warning / Error に昇格させない
- 新規 rule を追加するときは、必須 / 推奨 / 小技のどれに属するかをこの文書または対応仕様に明記する

## 6. 判断順序（再掲）

```text
1. UPD Core Rules（Error）を守る
2. 性能・メモリ・プラットフォーム制約を満たす
3. 可能な範囲で Recommended Structure（Warning）を採用する
4. Practical Tips（Attention）で可読性・運用性を改善する
```

性能上の理由で推奨事項を崩しても、Core Rules を守っていれば UPD 適合として扱います。
