# Layer Rules

**日本語** | [English](layer-rules.en.md)

この文書は、UI・Process・Dataの各層が担当する仕事と、必ず守る規則・採用を勧める構造・実装に役立つ小技を分けて説明します。たとえば商品検索では、UIが入力と表示、Processが検索条件の判断、Dataが商品情報の取得を担当します。3層の全体像は[Architecture](architecture.md)を参照してください。

## 規則の強さ

| 区分 | 意味 | 例 |
| --- | --- | --- |
| 必須（UPD Core） | 守らないとUPDの設計要件を満たしません。 | UIからDataへ直接アクセスしない。 |
| 推奨（Recommended） | 保守やテストをしやすくするため採用を勧めますが、UPDの成立条件ではありません。 | 1ファイル1責務を目指す。 |
| 小技（Practical Tip） | 理解やレビューに役立つ方法です。必須条件ではありません。 | 大きなファイルを責務分割の見直し材料にする。 |

この区分の正式な意味は[Guideline Levels](../specification/guideline-levels.md)に定義されています。このページでも、以下の見出しでどの強さの規則か分かるようにしています。

## 必須 — UPD Core Rules

次の責務境界はUPDの必須条件です。性能や実装環境に合わせて物理的なファイル・クラス分割を変えることはできますが、これらの境界は維持します。

### UI Layer

UI Layerは入力を受け付け、画面の状態や表示方法を決めます。UIフレームワークへの依存はこの層に置けます。

UI Layerは次の規則を守ります。

- ゲームや業務のルール、Processの数値計算を担当しません。
- Data Layerへ直接アクセスしません。データが必要な場合はProcessを経由します。
- Process Layerなど、別層のProcessingを直接呼び出しません。
- 画面色・座標・フォントなどの表示判断をProcessへ持ち込みません。

### Process Layer

Process Layerはアプリケーションのルール、検索条件の検証、計算、状態遷移など、結果の意味を決めます。

Process Layerは次の規則を守ります。

- UIフレームワークや画面色・座標・フォントなどの表示方法に依存しません。
- Dataのファイル形式、データベース、保存先などの実装詳細に依存しません。
- Data LayerのProcessingを直接呼び出しません。データが必要な場合はCommanderへ要求の結果を返し、Messenger経由の通信を依頼します。

### Data Layer

Data Layerはデータの取得・保存と、それに必要な形式変換を担当します。

Data Layerは次の規則を守ります。

- UIの表示方法を判断しません。
- アプリケーションやゲームのルールを判断しません。
- Process LayerのProcessingを直接呼び出しません。

### CommanderとMessenger

- Commanderは適切な処理や通信を選び、呼び出し順を整えます。独立した計算・描画・保存などの実処理を抱え込みません。
- Messengerは層を越える要求と結果を運びます。通信経路の中で業務判断や表示判断などの実処理をしません。
- 別層との要求・結果はMessengerを経由します。ある層のProcessingから別層のProcessingを直接呼び出しません。

## 推奨 — Recommended Structure

次の構造は責務の追跡、変更、テストをしやすくするための推奨です。採用しなくても、それだけでUPDの必須条件違反にはなりません。

- 1ファイル・1モジュールを、それぞれ主に1つの責務にします。
- クラスを使う場合は、1クラスの責務を小さく保ちます。
- 1関数・1メソッドは、主に1つの動作を担当するようにします。
- 処理が複数の独立した仕事へ膨らんだら、それぞれの責務に分けることを検討します。
- Namespace・package・moduleの論理構造とフォルダ構成を、可能な範囲で対応させます。

性能、メモリ、ビルド方式、プラットフォーム、既存資産などと競合する場合は、これらの推奨を採用しない判断ができます。推奨を外しても、上の必須境界は守ります。

## 小技 — Practical Tips

- コメントではコードの逐語訳より、「入力検証」「結果返却」など処理の目的や区切りを示します。
- 大きなファイルや型を見つけたら、行数だけで違反と決めず、責務が混ざっていないかレビューします。
- CheckerのWarningやAttentionは設計を考える手がかりとして読み、該当する規則が必須か推奨かを確認してから対応を決めます。

## 詳細

- 各層の正式な責務 → [Layer Specification](../specification/layer-spec.md)
- 規則の強さの正式な定義 → [Guideline Levels](../specification/guideline-levels.md)
- CommanderとMessengerの役割 → [Commander & Messenger](commander-messenger.md)
