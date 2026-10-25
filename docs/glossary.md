# UPD日本語用語集

UPDを初めて読む人向けに、設計とCheckerで使う言葉を説明します。例は「商品を検索して一覧に表示する機能」を中心にしています。各文書の用語リンクから、必要な項目だけを読めます。

用語の意味を確認するための文書です。必須・推奨・小技の区別は[Guideline Levels](../specification/guideline-levels.md)、Checkerの分類は[Checker Severity Boundary](../specification/checker-severity.md)を参照してください。

## 層と役割

<a id="layer"></a>

### Layer（層）

仕事を「何に関するものか」で分けた領域です。UPDではUI・Process・Dataの3層を使います。フォルダやクラスを必ず3つ作るという意味ではなく、それぞれが担当する仕事と通信経路を分ける考え方です。

<a id="role"></a>

### Role（役割）

各層の中で「どのような動きを担当するか」を表します。Commander・Messenger・Processingの3役割があります。層と役割は別の軸なので、たとえばUIにもDataにもProcessingがあります。

<a id="ui"></a>

### UI（利用者とのやり取り）

入力を受け取り、画面の状態や表示方法を決める層です。商品検索なら、入力欄からキーワードを読み、結果を一覧やカードにして表示します。検索条件の業務上の妥当性や保存方式は担当しません。

<a id="process"></a>

### Process（業務・ゲームのルール）

入力の検証、計算、業務やゲームのルールに基づく結果の決定を担当する層です。商品検索なら検索条件を検証します。画面の部品やデータベースの保存方式には依存しません。

<a id="data"></a>

### Data（データの取得・保存）

データベース、ファイル、外部データの読み書きや、取得・保存に必要な変換を担当する層です。商品検索なら商品情報を読み出します。表示方法や業務・ゲームのルールは判断しません。

<a id="commander"></a>

### Commander（呼び出しの整理）

同じ層で次に呼ぶ処理と順序を選ぶ役割です。要求をProcessingへ渡し、その結果に応じて次の処理やMessengerを呼びます。検索条件の検証や価格計算などの実作業はProcessingへ置きます。

<a id="messenger"></a>

### Messenger（層を越える通信）

隣り合う層の間で要求や結果を運ぶ役割です。Communicatorと記す文書もあります。受信内容を同じ層のCommanderへ渡します。直接メソッドを呼ぶ方式でも構いません。Messengerという名前から、キューや非同期処理が必須だと解釈しないでください。

<a id="processing"></a>

### Processing（実処理）

担当層に属する実際の作業を行う役割です。「実処理」「処理モジュール」と呼ぶ場合もあります。UIでは表示、Processでは検証や計算、Dataでは読み書きが例です。Processは層の名前、Processingは役割の名前です。

層と役割の詳細: [Architecture](architecture.md)、[Commander & Messenger](commander-messenger.md)、[Data Flow](data-flow.md)。

## 機能のまとまりと共有領域

<a id="application"></a>

### Application（独立した機能のまとまり）

UI・Process・Dataが1組として閉じる機能の範囲です。実行ファイルや製品全体と必ず一致するわけではありません。同じ検索処理と保存先を使う一覧画面と詳細画面は、同じApplication内の複数UIとして扱えます。

<a id="sub-application"></a>

### Sub Application（内側の独立した機能）

製品や上位Applicationの中に置かれた、独立したApplicationです。自身のUI・Process・Dataを持ちます。画面が別という理由だけで分割せず、処理・状態・保存が独立しているかで判断します。分割自体は推奨で、不要な階層を増やす必要はありません。

<a id="boundary"></a>

### Boundary（境界）

担当範囲の内側と外側を分ける線です。層の境界ならUIとProcessの間、Applicationの境界なら商品検索機能と独立した設定機能の間が例です。境界を越えて内部処理を直接使わず、定められた通信経路と受け渡し形式を使います。

<a id="common"></a>

### Common（層に属さない共有資産の領域）

複数の層などから利用する、データ型・定数・通信形式などを置く領域です。UI・Process・Dataに並ぶ第四層ではありません。表示・業務判断・保存などの層固有の仕事を移して、通信経路の規則を回避する場所でもありません。

<a id="shared"></a>

### Shared（共有資産の領域の別名）

このリポジトリではCommonと同じく、層に属さない共有資産を置く領域として扱います。名前だけで公開範囲は決まりません。特定Applicationの内側にある共有資産と、製品全体で公開する通信契約は区別します。

機能境界と共有領域の詳細: [Application Boundary](../specification/application-boundary.md)、[Common / Shared Specification](../specification/common-shared-spec.md)。

## 通信で受け渡すもの

<a id="message"></a>

### Message（メッセージ）

通信で運ぶ1件の要求または結果です。商品検索なら「この条件で商品を探してほしい」という要求と、商品一覧を返す結果が例です。メッセージという言葉だけで、通信手段や同期・非同期は決まりません。

<a id="contract"></a>

### Contract（契約）

複数の部品やツールが同じ意味で扱うためにそろえる、入力・出力・設定の形や振る舞いについての取り決めです。メッセージの項目や要求と結果の対応を定めるものはMessage Contractと呼びます。Checkerの出力形式や設定ファイルの規則も契約の一種で、すべてが層間通信の契約という意味ではありません。

<a id="dto"></a>

### DTO（受け渡し用のデータ型）

Data Transfer Objectの略です。通信や処理の受け渡しに必要な値をまとめる型で、構造体やrecordなどでも表現できます。検索キーワードと件数上限を持つ検索条件型が例です。UI操作やDB接続などの実処理を持たせるものではありません。

<a id="payload"></a>

### payload（運ぶデータの本体）

要求や結果の中で、実際に処理へ渡す値です。商品検索なら検索条件や商品一覧が例です。要求の識別子などの配送情報と区別します。画面部品やDB接続そのものを渡さず、相手の層が必要とする最小限の情報にします。

通信内容の詳細: [Message Contract](../specification/message-contract.md)。Checkerの契約の例: [Finding Baseline Contract](../specification/baseline-contract.md)、[Checker Regression Matrix](../specification/checker-regression-matrix.md)。

## Checkerの出力とCI

<a id="checker"></a>

### Checker（設計を調べるツール）

ソースコードや設定を読み、UPDの規則に関係する箇所を報告するツールです。機械的な検出結果を示すもので、プログラムのあらゆる設計判断や実行時の正しさを保証するものではありません。

<a id="rule"></a>

### Rule（判定規則）

Checkerが何を調べ、どの条件で報告するかを定めた規則です。たとえば`UPD101`は層を越える不正な依存を調べます。Rule IDは規則を識別する名前で、個々の報告や重要度とは別のものです。

<a id="finding"></a>

### Finding（検出した箇所の報告）

あるRuleに該当するとCheckerが判断した、個別の報告です。通常は規則ID、対象ファイル、行、説明などを含みます。同じRuleでも複数の箇所が該当すれば、複数のFindingが出ます。

<a id="severity"></a>

### Severity（報告の分類）

Findingが必須違反・推奨違反・小技のどれに対応するかを示します。現行の正本で使う分類はError・Warning・Attentionです。数値が大きいほど危険、といった独自の意味を推測せず、次の定義と規則の説明を確認します。

<a id="error"></a>

### Error（設計違反と実行失敗の文脈）

severityとしてのErrorは、UPDとして成立するための必須規則を破ったFindingの分類です。通常のCIでは既定の失敗条件になります。一方、config errorや解析エラーという文脈では、設定不正や解析失敗など、ツール自体の実行上の失敗を指します。設定や解析の失敗が、必須違反のFindingそのものを意味するわけではありません。

<a id="danger"></a>

### Danger（現行の分類では使わない表記）

Issueや一部の説明に登場しますが、現行の[severity正本](../specification/checker-severity.md)にDangerという分類はありません。必須違反の分類はErrorを参照します。Dangerを第四の分類や設定値として追加する意味ではありません。

<a id="warning"></a>

### Warning（推奨違反の分類）

UPDの必須条件は満たしていても、保守性や読みやすさなどを改善できる箇所を示す分類です。一般プロジェクトでは既定のCI失敗条件ではありません。性能や既存構成の事情から、理由を明らかにして採用しない判断もできます。

<a id="attention"></a>

### Attention（小技・軽微な改善候補の分類）

実装や運用を扱いやすくする、補助的な改善候補を示す分類です。たとえば値のまとめ方や配置の見直し候補があります。一般プロジェクトでは既定のCI失敗条件ではありません。

<a id="gate"></a>

### Gate（CIの合否条件）

Checkerの結果を使って、CIを成功・失敗のどちらにするか決める条件です。`fail_on`などで設定します。Findingを報告することと、CIを失敗にすることは別です。Gateの例外で失敗を許容しても、該当Findingは報告に残ります。

分類・合否の詳細: [Checker Severity Boundary](../specification/checker-severity.md)。

## 値をまとめる補助

<a id="compresser"></a>

### Compresser（受け渡す値をまとめる補助）

引数や返却値が増えたとき、Containerというデータのまとまりを作るための補助です。検索条件や検索結果を1つの受け渡し単位に整え、CommanderやMessengerが個々の値を扱う量を減らします。一般的なファイル圧縮の意味ではありません。導入は推奨で、UPDの必須条件ではありません。

<a id="container"></a>

### Container（入力・出力のまとまり）

ある処理へ渡す入力、またはその処理から返る値をまとめたものです。たとえばキーワードと件数上限をSearchInputContainerにまとめます。複数のContainerを1通信で送るためにまとめたものを、仕様ではPackageとも呼びます。ここでのContainerはDockerなどの実行環境を指しません。

値のまとめ方の詳細: [Compresser Specification](../specification/compresser-spec.md)。必須・推奨の区別は[Guideline Levels](../specification/guideline-levels.md)を優先します。

## 仕事と実装の単位

<a id="responsibility"></a>

### responsibility（責務）

その部分が引き受ける、まとまりのある仕事です。「検索条件を検証する」「商品情報を読み出す」などが例です。行数やメソッド数そのものではありません。責務を小さく保つことは推奨で、1ファイル1責務などの物理的な分割をすべて必須にする意味ではありません。

<a id="module"></a>

### module（モジュール）

コードをひとまとまりとして扱う単位です。言語に応じてファイル、package、namespace、クラス、関数群などで表現します。UPDはクラスを必須にしません。どの形でも、担当する仕事と依存先が読み取れるようにします。

実装単位の詳細: [Guideline Levels](../specification/guideline-levels.md)、[Responsibility Check Rules](../specification/responsibility-check-rules.md)。
