# Flat Layer Checker Rules

**日本語正本** | [English](flat-layer-check-rules.en.md)

本書は、UI / Process / Data 配下が大規模にもかかわらず平坦で、探索性が低下している場合に出す Attention の判定契約を定義する。

この規則は UPD 適合性の必須条件ではない。小規模な平坦構成や、性能・ビルド方式・既存資産等の理由で意図的に平坦化した構成を禁止しない。

## 1. Rule

- Rule ID: `UPD405`
- Severity: `attention`
- 目的: 大規模な層で、ほぼ全てのソースが層 root 直下へ集中している場合に、探索性改善の検討を促す
- 既定 CI: blocking しない

診断例:

```text
UPD405 large flat layer reduces navigability; consider grouping related responsibilities
```

具体的なフォルダ名、階層数、Application 分割方法は強制しない。

## 2. 対象

各 Checker が認識した Application / Sub Application ごとに、次の layer root を独立して判定する。

- UI
- Process
- Data

同じ物理ディレクトリ配下でも、別 Application / Sub Application と分類されたファイルは親 Application の集計へ含めない。

Application 境界を識別できない場合は、現在の分類 root 内で認識できる UI / Process / Data ごとに判定する。

## 3. eligible source file

集計対象は、その言語 Checker が通常の走査対象として扱うソースファイルである。

次は母数から除外する。

- 既存 CLI / config / `.updcommanderignore` により path ignore されたファイル
- パス要素に次の既定除外ディレクトリ名を含むファイル
  - `generated`
  - `third_party`
  - `vendor`
  - `external`
  - `build`
- 別 Application / Sub Application に属するファイル

ディレクトリ名の比較は各プラットフォームの通常の path 比較規則に従う。

## 4. 規模と平坦度

layer root に属する eligible source file を次の2種類へ分ける。

- `direct_files`: 親ディレクトリが layer root そのものであるファイル
- `nested_files`: layer root より下のサブディレクトリにあるファイル

`total_files = direct_files + nested_files` とする。

既定値:

```text
flat_layer_min_files = 12
flat_layer_min_direct_percent = 80
```

UPD405 は、次を**両方**満たす場合だけ出す。

```text
total_files >= flat_layer_min_files
direct_files * 100 >= total_files * flat_layer_min_direct_percent
```

浮動小数点計算は使用せず、上記の整数比較を正本とする。

### 4.1 境界

既定値では:

- 11ファイル以下: 平坦でも出さない
- 12ファイル、direct 9 / nested 3: 75% → 出さない
- 12ファイル、direct 10 / nested 2: 約83% → Attention
- 12ファイル、direct 12 / nested 0: 100% → Attention

「サブディレクトリが1個でもあれば安全」とは判定しない。大部分が root 直下へ残っている場合は平坦とみなす。

## 5. Config 契約

`config/path.json` に次を追加できる。

```json
{
  "flat_layer_min_files": 12,
  "flat_layer_min_direct_percent": 80
}
```

### flat_layer_min_files

- 正の整数
- 既定値: `12`
- `0`、負数、真偽値、文字列、小数、`null` は config error

### flat_layer_min_direct_percent

- `1` 以上 `100` 以下の整数
- 既定値: `80`
- 範囲外、真偽値、文字列、小数、`null` は config error

設定値の意味は Python / Go / C++ / C# で同一にする。

## 6. Finding 位置

UPD405 は layer root ごとに最大1件とする。

- path: scan root から見た layer root の相対パス
- line: `1`
- severity: `attention`

個々のファイルへ重複して finding を出してはならない。

## 7. 共通 fixture 契約

共通fixtureの数値契約は [flat-layer-fixtures.json](flat-layer-fixtures.json) を正とする。

必須ケース:

| Case | eligible total | direct | nested | excluded | Expected |
|---|---:|---:|---:|---:|---|
| small-flat | 8 | 8 | 0 | 0 | no finding |
| large-nested | 12 | 4 | 8 | 0 | no finding |
| large-flat | 12 | 12 | 0 | 0 | attention |
| generated-heavy | 5 | 5 | 0 | 20 | no finding |
| boundary-below-percent | 12 | 9 | 3 | 0 | no finding |
| boundary-over-percent | 12 | 10 | 2 | 0 | attention |

各言語のfixtureは構文だけを言語に合わせ、この file count / directory placement / expected severity を一致させる。

## 8. 非目標

この rule は次を判定しない。

- 個々のクラスやファイルの責務過多
- Namespace / package / module 名の妥当性
- 特定のフォルダ名を採用したか
- Application / Sub Application に分割すべきかどうか
- 平坦構成そのものが UPD 違反かどうか

責務過多は UPD401 等、Application 境界は既存の境界規則で別に扱う。
