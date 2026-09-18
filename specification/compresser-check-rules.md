# Compresser / Container Checker Rules

本書は `compresser-spec.md` に基づく静的チェックの重要度と判定方針を定義する。

## 1. Container 化は必須規定ではない

1クラスの入力を1つの Input Container、返却情報を1つの Output Container にまとめることは推奨するが、一般プロジェクトに対して必須とはしない。

理由は、Container / Package の生成、格納、取り出し、必要に応じたシリアライズ等そのものにも処理・実装上のコストが存在するためである。

したがって、単に複数引数または複数返却値を使用しているだけでエラーとしてはならない。

## 2. Attention

個別のクラス操作に複数入力または複数返却値が存在し、Container 化によって可読性を改善できる可能性がある場合は `attention` とする。

```text
A UPD301 ... multiple inputs reduce readability; consider one Input Container
A UPD302 ... multiple return values reduce readability; consider one Output Container
```

Attention は設計改善候補の提示であり、適合違反ではない。

Container 化による効果が小さい場合や、パッケージ化コストの方が大きい場合は、Attention を確認した上で現在の実装を維持してよい。

### 2.1 対象 callable

UPD301 / UPD302 はアクセス可視性ではなく、型・モジュールの操作として定義された callable を対象とする。

- public / exported method だけでなく private / protected / unexported method も対象とする。
- constructor は初期化責務であり通常の操作とは分けるため、UPD301 / UPD302 の対象外とする。
- Python は `__init__` と `__new__` を constructor / construction hook として対象外にするが、`_run` のような private 相当methodは対象とする。
- C# / C++ は言語構文上の constructor を対象外にする。
- Go は receiver method を可視性に関係なく対象とし、package-level function は対象外とする。`NewX` 等の命名だけからconstructor判定は行わない。

この範囲は「公開APIだけを整える」ためではなく、実装内部を含む操作単位の可読性を揃えるための規則である。

### 2.2 UPD301 の有効入力数

UPD301 は、callable の**有効入力数が設定された上限を超えた場合だけ** `attention` を出す。

既定上限は **2入力** とする。したがって既定設定では次の境界になる。

| 有効入力数 | 既定結果 |
|---:|---|
| 0 | findingなし |
| 1 | findingなし |
| 2 | findingなし（境界値） |
| 3以上 | `UPD301` attention（境界+1以上） |

「2入力程度の自然な操作」まで機械的に Container 化候補へしないことが目的である。

#### 2.2.1 1入力として数えるもの

有効入力数は「呼び出しシグネチャ上の独立した引数スロット数」とする。値の型、大きさ、default値の有無、実行時に渡される要素数では変化させない。

| 形式 | 有効入力数 |
|---|---:|
| 通常の positional parameter | 1 |
| positional-only parameter | 1 |
| positional-or-keyword parameter | 1 |
| keyword-only parameter | 1 |
| optional / default付きparameter | 1 |
| variadic parameter / parameter pack | 宣言1つにつき1 |
| implicit receiver（`this`, Go receiver等） | 0 |
| Python method の receiver としての `self` / `cls` | 0 |

補足:

- Python の `*args` と `**kwargs` はそれぞれ1入力として数える。実行時に何個の値を受け取るかは数えない。
- Go の `func (T) Run(a, b int)` は `a` と `b` を別入力として数え、有効入力数は2とする。variadic `values ...T` は1入力とする。
- C# の `params T[]` は1入力とする。
- C++ の function parameter pack は1入力とする。
- constructor は 2.1 の規則によりUPD301の対象外なので、constructor parameter数は数えない。
- receiver以外の明示parameterは、`ref` / `in` / `out` 等の言語固有modifierやdefault値があっても1スロットとして数える。

Python の `self` / `cls` 除外は receiver に対して適用する。receiverを持たない static method では、明示parameterを通常どおり数える。

### 2.3 設定契約

UPD301 の上限設定キーは `upd301_max_inputs` とする。

```json
{
  "upd301_max_inputs": 2
}
```

意味は「UPD301を出さずに許容する最大の有効入力数」である。

- 省略時: `2`
- 有効値: **1以上の整数**
- `effective_inputs <= upd301_max_inputs`: findingなし
- `effective_inputs > upd301_max_inputs`: `UPD301` attention
- `null`, boolean, string, 小数、0以下: config error
- config error は既存の設定エラー処理に従い、解析を開始せず非0終了する

既存configにこのキーが無くても読み込み可能である。ただし、旧実装の「2入力からUPD301」を維持したい利用者は `upd301_max_inputs: 1` を明示する。

このキーはUPD301だけを制御する。UPD303の「大幅な圧縮」判定は独立したheuristicであり、`upd301_max_inputs` をそのままUPD303の発火境界として使ってはいけない。UPD303が入力数を参照する場合も、有効入力数の数え方だけを共有する。

### 2.4 局所例外

performance hot path、ABI/API互換、allocation回避など、特定箇所だけContainer化を避ける場合は、専用のUPD301例外構文を追加しない。

既存の inline ignore、path/rule ignore、CI gate等の局所例外機構を利用し、可能な場合は理由を残す。1箇所の例外のためにproject全体の `upd301_max_inputs` を引き上げることは推奨しない。

### 2.5 4言語共通の境界fixture

既定値 `upd301_max_inputs = 2` では、次のgood/bad境界を4言語で同じ期待値として扱う。

| Fixture | Effective inputs | Expected |
|---|---:|---|
| no-input | 0 | findingなし |
| one-input | 1 | findingなし |
| two-inputs | 2 | findingなし |
| boundary | 2 | findingなし |
| boundary-plus-one | 3 | `A UPD301` |
| one-fixed-plus-variadic | 2 | findingなし |
| two-fixed-plus-variadic | 3 | `A UPD301` |
| custom-max-1 / two-inputs | 2 | `A UPD301` |
| custom-max-3 / three-inputs | 3 | findingなし |
| custom-max-3 / four-inputs | 4 | `A UPD301` |

代表的な境界コードは次のとおり。どの言語でも `Good` は有効入力数2、`Bad` は3である。

```python
class Worker:
    def Good(self, left, right):
        pass

    def Bad(self, left, right, mode):
        pass
```

```go
type Worker struct{}

func (Worker) Good(left, right int) {}
func (Worker) Bad(left, right, mode int) {}
```

```cpp
struct Worker {
    void Good(int left, int right) {}
    void Bad(int left, int right, int mode) {}
};
```

```csharp
internal sealed class Worker
{
    internal void Good(int left, int right) { }
    internal void Bad(int left, int right, int mode) { }
}
```

## 3. Warning

Commander または Messenger について、Compresser / Container を導入することでクラス自体を大幅に圧縮できると推定される場合のみ `warning` を出す。

```text
W UPD303 ... Compresser/Container introduction is expected to substantially reduce this Commander/Messenger
```

Warning の目的は「複数引数があること」そのものを罰することではなく、多数の通信値の受け渡しによって Commander / Messenger が肥大化している状態を検出することである。

初期実装では、削減可能量の推定値が10行以上であり、かつ対象 Commander / Messenger の有効コード量のおおむね20%以上を削減できると推定される場合を「大幅な圧縮」とみなす。

この数値は設計思想そのものではなくチェッカーの初期ヒューリスティックである。誤検知・見逃しの実績に応じて調整してよい。

## 4. 削減可能量の推定

言語ごとの構文解析能力に応じて、次の情報から削減可能量を推定する。

- 複数入力の余剰値数
- 複数返却値の余剰値数
- 複数行に展開されたメソッド / 関数シグネチャ
- 対象クラスまたは対象ファイルの有効コード行数

高精度ASTを利用できる実装ではクラス単位の行数を優先する。

軽量パーサ / 正規表現ベースの実装では、対象UPDコンポーネントファイル単位の行数を近似値として使用してよい。

## 5. Compresser の実装粒度

Compresser は1クラスごとに分離しても、関連する複数クラスを機能単位でまとめてもよい。

ただし、機能単位Compresserが巨大化して可読性を損なう場合は分割する。

粒度の判断基準は「Compresserの数を最小化すること」ではなく、「1クラス1Containerを維持したまま読みやすい責務単位を作ること」とする。

## 6. チェッカー自身

UPD Commander Checker 自身は設計例として扱うため、通常利用者向けの許容基準より厳しくする。

チェッカー自身の実装では、Container 化可能な複数入力・複数返却値を原則として Container 化し、Self Check で Attention / Warning を残さないことを目標とする。

これは一般プロジェクトへ Container 化を強制する規定ではなく、設計ツール自身が推奨設計を実践するための自己適合規定である。
