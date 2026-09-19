# Model Attention Checker Rules

**日本語正本** | [English](model-attention-check-rules.en.md)

本書は、関連する複数値を同じ組として繰り返し羅列している場合に、Model / DTO / struct / record 等の意味単位へまとめられる可能性を示す Attention の判定契約を定義する。

この規則は Model 化を必須にしない。SoA、SIMD、連続メモリ配置、FFI、低レベル API、hot path 等、性能や ABI の理由で値を分離して保持する設計は正当であり、必要に応じて既存の rule-specific ignore で明示的に抑制できる。

## 1. Rule

- Rule ID: `UPD406`
- Severity: `attention`
- 目的: 3項目以上の同一値グループが2箇所以上で繰り返される場合に、意味あるデータ単位へまとめる余地を提示する
- 既定 CI: blocking しない

診断例:

```text
UPD406 repeated value group may benefit from a Model/DTO; items=id,name,email occurrences=2 kind=parameters
```

UPD406 は「Model を作らなければならない」という判定ではない。

## 2. 対象となる occurrence

Checker は次のいずれかを **value-group occurrence** として扱う。

### 2.1 parameter group

関数 / method / free function の有効入力値の並び。

- Python の `self` / `cls` は除外
- C# extension receiver は除外
- 言語が暗黙的に渡す receiver は除外
- 既定値の有無は signature を変えない
- variadic / params は1項目として数える

UPD301 と異なり、入力数が多いだけでは UPD406 を出さない。同一 group の反復が必要。

### 2.2 tuple / multi-value group

同じ文法単位でまとめて扱われる3値以上の並び。

例:

- tuple literal / tuple return
- destructuring / deconstruction
- Go の multi-value return / assignment
- C++ の `std::tuple` / structured binding 相当

言語ごとの表現差は許容するが、「同じ順序の値グループ」という意味を揃える。

### 2.3 parallel collection access group

同一 statement / expression 内で、同一 index / iterator により3本以上の collection が並行参照される並び。

例:

```text
x[i], y[i], z[i]
```

1 statement を1 occurrence とする。同じ group が別 statement でも再度現れた場合に反復として数える。

単に3本の collection が宣言されているだけでは evidence にしない。

## 3. item key と group signature

各 occurrence は、順序を保持した item key の列へ正規化する。

### 3.1 item key

- simple identifier: 識別子名
- member access: 最終 member 名
- indexed collection: collection 識別子名
- parameter: parameter 名

正規化は次だけを行う。

- case-insensitive 比較のため小文字化
- 先頭の `_` を除去

camelCase / snake_case の分割、単複変換、同義語推定、型名推定は行わない。

### 3.2 signature

```text
(application, layer, occurrence_kind, ordered_item_keys)
```

を group signature とする。

- 項目順が異なれば別 group
- occurrence kind が異なれば別 group
- 別 Application / Sub Application は別 group
- UI / Process / Data の別 layer は別 group
- Application / layer を識別できない場合は現在の scan/classification scope を使う

subset 探索はしない。4項目 group の中に含まれる任意の3項目を別候補として量産してはならない。

## 4. 発火条件

既定値:

```text
model_group_min_items = 3
model_group_min_occurrences = 2
```

UPD406 は、同一 signature について次を両方満たす場合だけ出す。

```text
item_count >= model_group_min_items
occurrence_count >= model_group_min_occurrences
```

既定では単なる2値組は対象外。

finding は signature ごとに最大1件とし、最初の eligible occurrence を位置として使う。

## 5. 既存 Model / DTO / aggregate の除外

次は occurrence evidence として数えない。

- user-defined Model / DTO / struct / record / dataclass / named tuple 等の **型定義内部で、field/member を列挙しているだけの箇所**
- 既に単一の user-defined aggregate 値として渡されている parameter / return / argument
- constructor / initializer に値を詰めるだけの箇所

既存型の method 内に、別の raw value group が現れた場合は通常どおり対象にできる。

つまり「型の中に3 field がある」こと自体は UPD406 の evidence ではない。

## 6. 性能・低レベルコードの扱い

Checker は「SoA らしい」「SIMD らしい」等を名前だけで推測して自動除外しない。

性能・ABI・interop 等の理由で意図的に分離配置する場合は、既存の rule-specific ignore を正規の抑制手段とする。

例:

```text
UPD406 process/hot_path/**
UPD406 data/soa/**
```

inline ignore をサポートする言語では、該当 occurrence の行に `upd: ignore UPD406` を指定できる。

この抑制は UPD406 の finding だけを消し、他 rule の解析対象から path 全体を除外してはならない。

generated / third_party / vendor / external / build 等、既存 path ignore / 除外で走査対象外のコードは当然 occurrence 母数にも含めない。

## 7. Config 契約

`config/path.json` に次を追加できる。

```json
{
  "model_group_min_items": 3,
  "model_group_min_occurrences": 2
}
```

### model_group_min_items

- 3以上の整数
- 既定値: `3`
- 2以下、負数、真偽値、文字列、小数、`null` は config error

### model_group_min_occurrences

- 2以上の整数
- 既定値: `2`
- 1以下、負数、真偽値、文字列、小数、`null` は config error

設定値の意味は Python / Go / C++ / C# で同一にする。

## 8. Finding evidence

finding には最低限、次を含める。

- `kind`: occurrence kind
- `items`: ordered item keys
- `occurrences`: eligible occurrence count

CLI の短い message では同一行に含めてよい。

位置:

- path: 最初の eligible occurrence の source path
- line: 最初の eligible occurrence の開始行
- severity: `attention`

追加 metadata を持てる実装では、2件目以降の path / line も evidence として保持してよい。

## 9. 共通 fixture 契約

共通fixtureの数値・期待契約は [model-attention-fixtures.json](model-attention-fixtures.json) を正とする。

必須ケース:

| Case | kind | items | occurrences | Expected |
|---|---|---:|---:|---|
| two-item-repeat | parameters | 2 | 3 | no finding |
| repeated-parameters | parameters | 3 | 2 | attention |
| one-off-parameters | parameters | 4 | 1 | no finding |
| repeated-tuple | tuple | 3 | 2 | attention |
| repeated-parallel-index | parallel_collection | 3 | 2 | attention |
| existing-model | aggregate_definition | 4 | 3 | no finding |
| performance-suppressed | parallel_collection | 3 | 3 | no finding |
| different-order | parameters | 3 | 2 signatures | no finding |
| cross-layer-only | parameters | 3 | 1 per layer | no finding |

performance-suppressed は `UPD406` rule-specific ignore を利用する。

各言語fixtureは構文だけを言語に合わせ、group signature と期待severityを一致させる。

## 10. 非目標

UPD406 は次を判定しない。

- 値の意味が本当に同一 business concept か
- どの型名 / class名 / record名を採用すべきか
- AoS と SoA のどちらが性能上有利か
- UPD301 の入力数しきい値
- UPD302 の複数返却値
- Model 化しないことが UPD 違反かどうか

UPD406 は、反復した形からレビュー候補を提示するヒューリスティックに限定する。
