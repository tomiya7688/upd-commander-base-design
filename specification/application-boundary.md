# Application Boundary

UPD Commander では、Application を単なる実行ファイルや製品全体ではなく、UI / Process / Data が1組として閉じる独立した機能境界として扱う。

## 1. Application の定義

次の条件を多く満たす機能は、独立した Application として扱うことを推奨する。

- 独自の画面群を持つ。
- 独自の処理フローを持つ。
- 独自の状態を持つ。
- 独自の保存・取得責務を持つ。
- 他画面・他機能の Processing を直接利用せず成立する。
- 他機能との通信を明示的な境界として定義できる。

画面が別であることだけでは Application 分割の十分条件ではない。表示だけが異なり、同一の Process / Data を共有する場合は同一 Application 内の複数 UI として扱ってよい。

## 2. Nested Application

1つの製品内に複数の独立機能が存在する場合、それぞれを Sub Application として分割し、各 Application 内で UI / Process / Data の3層を再度適用する。

```text
Product
├─ MainApplication
│  ├─ UI
│  ├─ Process
│  └─ Data
│
└─ SettingsApplication
   ├─ UI
   ├─ Process
   └─ Data
```

UPD Commander の3層構造は製品直下の1回だけではなく、独立した Application 境界ごとに再帰的に適用できる。

## 3. Application 間の依存

別 Application の内部実装へ直接依存してはならない。

禁止例:

```text
MainApplication/UI Processing
  -> SettingsApplication/Process Processing
```

```text
MainApplication/Process Processing
  -> SettingsApplication/Data Processing
```

Application 間で通信が必要な場合は、次のいずれかを使用する。

- 上位 Application / Product の Commander
- 明示された Messenger
- Application 間 Contract / DTO / Message
- 特定 Application に属さない Shared Contract

Shared 領域に Processing や保存処理を置いて Application 境界を回避してはならない。

## 4. 各 Application 内の原則

各 Application は独立して次の原則を満たす。

```text
Application
├─ UI
├─ Process
└─ Data
```

各層内では従来の Commander / Messenger / Processing 規則を適用する。

つまり、Application が複数存在しても、1つの巨大な UI / Process / Data を共有する構造を標準形とはしない。

## 5. 判定例

### 別 Application とする例

- メイン画面と設定画面で、処理・状態・保存が独立している。
- エディタとプレビューで、それぞれ独立した処理系を持つ。
- ランチャーとゲーム本体が、明示されたメッセージ境界だけで連携する。

### 同一 Application のままでよい例

- 同じ Process / Data を利用する一覧画面と詳細画面。
- PC画面とモバイル画面で表示方法だけが異なる。
- タブ切り替えだけで処理責務が共通している。

## 6. 推奨ディレクトリ例

```text
applications/
├─ main/
│  ├─ ui/
│  ├─ process/
│  └─ data/
└─ settings/
   ├─ ui/
   ├─ process/
   └─ data/
```

`apps/`、`applications/`、`features/` 等の名称はプロジェクト側で決めてよい。重要なのは、Application 境界がパスまたは設定から機械的に識別できることである。

## 7. ツール適合

静的チェッカーは、Application 境界を識別できる場合、各 Application ごとに UI / Process / Data を独立判定し、別 Application の内部層・Processing への直接依存を検出することが望ましい。

Application 境界を推定できない構成では、設定ファイル等による明示を許可する。
