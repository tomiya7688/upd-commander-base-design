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
