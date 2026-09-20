# Common / Shared Specification

**日本語正本** | [English](common-shared-spec.en.md)

本書は、UPD Commander における `Common` / `Shared` 領域の責務、依存方向、設定契約を定義する。

Common / Shared は UI / Process / Data に並ぶ第四層ではない。特定層へ所属しない、複数層または複数 Application から共有可能な**層中立資産領域**である。

## 1. 基本原則

通常の Application は引き続き次の3層で構成する。

```text
Application
├─ UI
├─ Process
└─ Data
```

必要な場合のみ、中立資産を次のように置いてよい。

```text
Application
├─ UI
├─ Process
├─ Data
└─ Common
```

または:

```text
Application
├─ UI
├─ Process
├─ Data
└─ Shared
```

これは層を4つへ増やすことを意味しない。

## 2. Common / Shared に置いてよいもの

次のような、特定層の処理責務を持たない資産を置ける。

- DTO
- Message / Contract
- value object
- enum / constant
- immutable data structure
- 複数層で利用される純粋な型定義
- 複数 Application 間の境界 contract
- framework非依存の小さな共通 utility
- 層に依存しない serialization schema / protocol schema

共通 utility は副作用や層固有I/Oを持たず、UI / Process / Data のいずれの責務も実装してはならない。

## 3. Common / Shared に置いてはならないもの

次は Common / Shared へ置いて規則を回避してはならない。

- UI描画・UIイベント処理
- 業務ロジック・ゲームロジック
- DB / file / network persistence
- UI / Process / Data Processing
- 層固有 Commander
- 層固有 Messenger
- 特定フレームワークへ密結合した実装
- 特定層専用なのに共有領域へ退避しただけの処理

つまり、`common` や `shared` という名前自体は責務の免罪符ではない。

## 4. 依存方向

依存の正本は次である。

| Source | Target | Allowed | Notes |
|---|---|---:|---|
| UI | Common/Shared | Yes | 中立contract/value利用 |
| Process | Common/Shared | Yes | 中立contract/value利用 |
| Data | Common/Shared | Yes | 中立contract/value利用 |
| Common/Shared | Common/Shared | Yes | 中立資産間の依存 |
| Common/Shared | UI | No | UI責務へ依存して中立性を失う |
| Common/Shared | Process | No | 業務/ゲーム処理へ依存して中立性を失う |
| Common/Shared | Data | No | 保存方式へ依存して中立性を失う |

Common / Shared は層間通信経路を置き換えない。

たとえば UI が Common DTO を参照できても、Common を経由して Data Processing を直接呼んではならない。

## 5. Application 境界

Common / Shared は配置場所に応じて scope を持つ。

### 5.1 Application-local Common

```text
applications/main/common/
```

この領域は `main` Application に属する中立資産である。

別 Application から直接利用することは原則として Application 内部参照とみなし、明示された inter-Application contract でない限り許可しない。

### 5.2 Product-level Common

```text
common/
shared/
```

Application 群の外側にある Common / Shared は、Product全体で共有する中立資産として扱える。

ここには特に次を置くことを想定する。

- inter-Application Contract
- shared DTO / Message
- 全Application共通の value object
- protocol / schema

Product-level Common も UI / Process / Data の内部実装へ依存してはならない。

## 6. Checker における分類

Checker は recognized common root 配下を `Common` と分類する。

`Common` は UI / Process / Data のいずれにも分類しない。

したがって:

- Common配下だからという理由だけで「層未所属」Errorを出してはならない
- Common配下を第四層としてUPDの3層必須条件へ追加してはならない
- Common -> UI/Process/Data の参照は Core Rule違反として扱う
- UI/Process/Data -> Common の参照は、他の境界違反がなければ許可する

## 7. path classification の優先順位

同一パスに複数の marker が現れる場合、最も内側の Application scope を決定してから、そのscope内で最も内側の recognized layer/common root を使う。

例:

```text
applications/main/common/contracts/message.cs
```

は `main` Application の Common。

```text
applications/main/process/common_helper.cs
```

はファイル名に `common` を含んでも Process であり、Commonではない。

root名は**ディレクトリ要素の完全一致**で判定する。

## 8. Config 契約

`config/path.json` に次を追加できる。

```json
{
  "common_roots": ["common", "shared"]
}
```

### 8.1 既定値

```text
common_roots = ["common", "shared"]
```

### 8.2 バリデーション

- 配列でなければ config error
- 要素は空でない文字列
- path separator を含めない
- `.` / `..` を許可しない
- UI / Process / Data の既定layer名と同じ値を許可しない
- 大文字小文字の比較は各platformの通常path比較規則に従う
- 重複値は正規化後に1件として扱ってよい
- 空配列は Common / Shared 自動認識を無効化する

設定値の意味は Python / Go / C++ / C# で同一にする。

## 9. 既存設定との互換性

`common_roots` を指定しない既存設定は、既定値 `["common", "shared"]` を使用する。

既存の:

- `ignore`
- `.updcommanderignore`
- `enabled_rules`
- CLI path override

の意味は変更しない。

ignore対象のCommon資産は通常走査と同様に解析対象外となる。

## 10. 例外

Commonから層固有実装への依存を、単に便利だからという理由で例外化してはならない。

どうしても必要な場合は既存の例外方針に従い、少なくとも次を記録する。

- 理由
- 対象path / symbol
- 代替案
- 除去条件

ただしCommonの中立性を恒久的に破る例外は、Common配置自体を見直すべきである。

## 11. 非目標

本仕様は次を定義しない。

- Common資産が本当に複数層で使われているか
- 1層専用Common資産へのWarning
- 言語ごとのimport/include/reference抽出方法
- package/namespace/module命名規則

「実質1層専用のCommon資産」判定は #83 系列で別途定義する。

## 12. 実装判定表

| Scenario | Expected |
|---|---|
| UI -> Common DTO | allowed |
| Process -> Common value object | allowed |
| Data -> Common schema | allowed |
| Common contract -> Common value object | allowed |
| Common -> UI rendering implementation | Core Rule violation |
| Common -> Process business processing | Core Rule violation |
| Common -> Data repository implementation | Core Rule violation |
| Application-local Common used as another Application's internal implementation | cross-Application violation |
| Product-level Common contract used by multiple Applications | allowed |
| source under recognized Common root | not an unassigned-layer error |
