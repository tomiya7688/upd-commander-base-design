# 配布物の署名・真正性検証の設計

この文書は [Issue #221](https://github.com/tomiya7688/upd-commander-base-design/issues/221) の実装設計と進捗の正本です。Artifact Attestationの部分検証とWindows単体PE署名の部分検証は実装済みですが、全成果物を結ぶ公開ゲート・信頼された公開者の設定・失効確認の検証は未完了です。Issue #221が完了するまで、未署名または真正性を検証できない成果物を公開しません。Pythonの証明書対応 [#220](https://github.com/tomiya7688/upd-commander-base-design/issues/220) も別途必要です。

## 対象と現状

| 経路                                                              | 現在の出力                                                          | 公開時の対象                                            |
| ----------------------------------------------------------------- | ------------------------------------------------------------------- | ------------------------------------------------------- |
| Python `support_tools/python/upd_commander_checker/build_exe.bat` | CUIのone-dir、GUIのone-file、設定ファイル                           | 個々のWindows実行ファイルと、それらをまとめた最終配布物 |
| Go `support_tools/go/upd_commander_checker/build_exe.bat`         | Windows実行ファイルと設定ファイル                                   | 実行ファイルと最終配布物                                |
| C++ `support_tools/cpp/upd_commander_checker/build_exe.bat`       | Windows実行ファイル、設定、条件により`libclang.dll`、ライセンス文書 | 実行ファイル・同梱DLLと最終配布物                       |
| C# `support_tools/cs/upd_commander_checker/build_exe.bat`         | Windows向けself-contained single-file出力とライセンス文書           | 実行ファイルと最終配布物                                |

これらはビルド経路の棚卸しであり、現在公開されている資産の一覧ではありません。GitHub Releases以外も含む既存配布物の所在・署名・対応状況は公開担当が確認し、検証できないものを明示する必要があります。CIのテスト用出力やローカルの開発ビルドが、公開用の署名済み成果物になるわけではありません。

### 現行の公開状況（2026-11-08確認）

確認対象のソースはmainのコミット `c60af49b525747ab7e3abca14aa8841765b13001` です。GitHub APIで確認した結果、GitHub Releasesは0件でした。これはGitHub Releases上に公開資産がないことだけを示し、GitHub外のダウンロードサイト、パッケージレジストリ、第三者ホスティングの不存在を示しません。

mainの `.github/workflows` には `attestation-integration.yml`、`checker-self-check.yml`、`ci.yml`、`csharp-responsibility-thresholds.yml` があります。リリース公開用workflowは確認できませんでした。Attestation integrationは一時ディレクトリ内の `fixture.bin` と `SHA256SUMS` を使うテスト用であり、製品ビルドや公開資産ではありません。前掲のPython・Go・C++・C#の出力は開発用ビルド経路として記録し、現在の公開物とは扱いません。

| 確認項目                | 2026-11-08時点の確認結果 | 範囲と残る確認                                                   |
| ----------------------- | ------------------------ | ---------------------------------------------------------------- |
| GitHub Releases         | 0件                      | GitHub Releases APIでの確認。リポジトリ外は対象外                |
| リリース用workflow      | 確認できず               | mainの `.github/workflows` 4ファイルを確認                       |
| Attestation integration | テストfixtureのみ        | `fixture.bin` と `SHA256SUMS`。製品・公開資産ではない            |
| GitHub外の配布先        | 未確認                   | サイト、レジストリ、第三者ホスティング、旧版を公開担当が調査する |

したがって、Issue #221の「現在サポート中の最新版release / 配布物」の調査と既存成果物の対応状況は未完了です。配布先と最新版を特定して実物を取得するまでは、外部の公開物が存在しない、または安全であるとは判断しません。未署名・真正性未確認の成果物を新規公開しない方針を維持します。

### GitHub外の配布先を探した結果（2026-11-08）

調査時点の基準は既定ブランチの `88cc13c82539f44b7ac67efc3aa6646ca2809e0c` です。公開README、リポジトリ内の配布・公開設定、GitHubの公開情報、および製品名によるWeb検索を確認しました。検索結果に現れないことは、配布物が存在しない証拠ではありません。

- [GitHub Releases](https://github.com/tomiya7688/upd-commander-base-design/releases) には調査時点でリリースがありません。
- 日本語・英語READMEにダウンロード先はありません。Python CheckerのREADMEは開発・検証用のビルドを説明し、署名済み公開リリースや公開アーカイブを提供していないとしています。
- リポジトリの既定ブランチを対象にPyPI、NuGet、Docker、wingetへの公開設定を検索しましたが該当は見つかりませんでした。GitHubリポジトリ検索もこのリポジトリ以外の候補を返しませんでした。いずれも検索範囲に限られ、網羅性はありません。
- 製品名・リポジトリ名によるWeb検索でも該当する公開配布ページは見つかりませんでした。個人サイト、クラウドストレージ、非公開・非索引のレジストリやミラーは確認できていません。
- GitHubのタグ一覧と最新タグは確認できていません。タグページの取得と `git ls-remote --tags` はネットワーク接続エラーとなりました。

このため、現在サポート中の最新版、GitHub外の配布先、既存の実配布物は未特定です。次は所有者が管理するサイト・レジストリ・ストレージを確認し、配布物を入手できた場合に形式、版、SHA-256、署名とattestationを記録します。新たな未署名・未検証成果物を公開しない方針を維持します。

### 所有者アカウントの公開情報を確認した結果（2026-11-08）

GitHubの[所有者プロフィール](https://github.com/tomiya7688)では、確認時にPackagesの表示が0で、ページ本文に外部サイトへのリンクは見当たりませんでした。これは公開プロフィール上で確認できた表示に限られ、別アカウントや非公開の配布場所を否定するものではありません。

Python Checkerのパッケージ定義（`support_tools/python/upd_commander_checker/pyproject.toml`）に記載された配布名は `upd-commander-checker`、CLI名は `upd-commander-check` です。これらの[PyPIプロジェクトページ](https://pypi.org/project/upd-commander-checker/)を確認しようとしましたが、参照ツールでページを取得できませんでした。取得失敗を未公開の証拠とは扱いません。NuGet・npm・Docker Hubの検索ページ、GitHub PagesのプロジェクトURLも同じく参照ツールから取得できず、公開状態は確認できていません。

既定ブランチのコード検索では `deploy-pages`、`github-pages`、`gh-pages` に該当する設定は見つかりませんでした。GitHub PagesのURL自体にもアクセスできなかったため、Pagesが無効だとは結論しません。今回確認できた所有者管理の外部サイト・レジストリ・ストレージ、および取得可能な実配布物はありません。公開プロフィールや検索から分からないサイト・アカウント・保管場所は未確認のままです。

## Windows署名検証の現状

`support_tools/python/upd_commander_checker/scripts/verify_windows_signature.py` は、Windows上の単一の`.exe`または`.dll`に対する部分検証コマンドです。Windows Authenticode APIで埋め込み署名、状態、署名者証明書SHA-256、timestamp証明書を読み、指定された信頼済み`signtool.exe`の`verify /pa /all /tw`出力で単一のSHA-256署名とRFC 3161 timestampを確認します。証明書のSHA-256値は、公開者を別途承認した管理済み情報から指定します。

実行例（PowerShell。SDKに含まれる信頼済みSignToolの絶対パスと、独立した手順で承認した証明書指紋を使う）:

```powershell
$signerSha256 = 'REPLACE_WITH_APPROVED_CERTIFICATE_SHA256'
$signtool = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe'
python .\support_tools\python\upd_commander_checker\scripts\verify_windows_signature.py `
  .\support_tools\python\upd_commander_checker\dist\app.exe `
  --signer-sha256 $signerSha256 `
  --signtool $signtool `
  --trust-immutable-file
```

`$signerSha256`には独立して承認した証明書SHA-256を設定し、`$signtool`は実際に導入済みSDKの信頼済みSignTool絶対パスへ合わせます。

呼出元は検証中にファイルと全親パスを変更できない状態に保ちます。ツールはUNCパスとリモートドライブを拒否しますが、競合するローカル書換えを防止せず、同梱ファイル・インストーラー・アーカイブ・attestationを棚卸しません。OSが返す署名状態を使いますが、失効情報へのオンライン到達性やオフライン時の失効確認失敗動作は未検証です。Windows CIでは一時的な自己署名・timestampなしのビルド成果物を作り、Windowsが埋め込み署名者とtimestamp不在を報告すること、および検証器がその未信頼のテスト署名を拒否することを確認します。CI runnerの信頼ストアへ証明書を追加しないため、検証器がtimestamp不足の分岐まで到達することや、公開用証明書の信頼、RFC 3161 timestamp付き署名の受け入れは証明しません。これらを含むリリースゲートと公開者の信頼ルートが整うまでは、Issue #221は未完了であり、一般公開を許可しません。

公開形態がZIP以外、wheel、コンテナ、他OS向けに増える場合は、実際にユーザーへ渡す最終形式を棚卸しに追加し、その形式の署名・検証ゲートができるまで公開しません。

## 公開経路とゲート

1. 保護されたリリース用workflowが、対象コミットと依存物を確定し、新しい隔離ディレクトリでビルド・テストする。Pythonは #220 の修正版環境、実際の信頼源、生成物の確認も通す。既存の`build/`や`dist/`を持ち越さない。
2. このプロジェクトが生成したWindowsの配布対象EXE・DLLを、信頼されたコード署名証明書でAuthenticode署名し、RFC 3161タイムスタンプを付ける。署名鍵はリポジトリやログへ置かない。証明書の取得・保管・利用権限・失効時の運用は別途確立する。
3. 配布対象の各PEファイルについて署名、証明書チェーン、発行者とタイムスタンプをWindows上で検証する。第三者製DLLは入手元・ハッシュ・ライセンスと、適用する署名検証方針を記録する。第三者の署名を自分の署名に置き換えたことを、第三者由来の証明と扱わない。検証できないバイナリは配布しない。
4. 検証済みのバイナリ、設定、必要なライセンス文書をまとめる。アーカイブのファイル一覧を固定し、想定外・未検証の実行可能ファイルや、パス外を指すエントリがあれば失敗させる。署名後のバイナリは変更しない。
5. 最終アーカイブのSHA-256を計算し、全公開資産を列挙する`SHA256SUMS`を作る。各最終アーカイブと`SHA256SUMS`自体にGitHub Artifact Attestationを付与する。ハッシュ一覧だけでは発行元を証明しない。
6. 別の検証ステップで、配布直前の**同じバイト列**についてSHA-256とattestationを検証する。リポジトリだけでなく、想定する署名workflowの識別と対象コミットを固定し、想定外のworkflow・コミットや検証失敗を拒否する。Windowsバイナリの署名検証も再確認する。
7. 検証済みの各ファイルの名前・SHA-256を公開処理に渡す。GitHub Releasesではドラフトへアップロードし、ドラフトから再取得した各資産のハッシュとattestationを照合してから公開する。形式上ドラフト内で同じ検証ができない配布先は、その条件を満たす方法ができるまで使わない。公開後も再取得して照合し、不一致なら公開完了と扱わず、速やかに配布停止・利用者への通知を行う。公開後の差し替えも同じゲートを通す。

GitHub ActionsからのattestationにはOIDCを使用し、必要最小限の権限をビルドと公開の各段階に分けます。`gh attestation verify`は対象リポジトリと署名workflowなどを指定できます。詳細は[GitHub CLIの検証仕様](https://cli.github.com/manual/gh_attestation_verify)と[GitHubのattestation手順](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations)に従い、実装時点の仕様を確認してください。attestationはAuthenticode署名、タイムスタンプ、Windowsでの発行者検証の代替ではありません。

署名者の信頼は、OSがチェーンを信頼することだけでは決まりません。運用開始前にプロジェクトが許可する署名者と証明書のSHA-256識別子を保護された設定に登録し、第三者バイナリには別の承認済み入手元と識別子を設けます。証明書更新は承認・監査し、有効期間の重複や旧証明書の扱いを明示します。失効情報を取得できない場合も公開ゲートを失敗させます。有効なタイムスタンプがある署名の期限後検証は、署名時点と失効状況を含むWindowsの検証結果に従います。

利用者向け手順では、信頼できるプロジェクトの告知から期待するリポジトリ、署名workflow、対象コミット、Windows署名者の識別子を得たうえで、ダウンロードしたアーカイブと`SHA256SUMS`それぞれに`gh attestation verify`を実行します。`--repo`、`--signer-workflow`、`--source-digest`に期待値を指定し、検証後にハッシュを照合します。コマンドと実際の識別子は公開基盤の確定後にREADMEへ記載し、失敗・未確認の場合は使用しないよう案内します。配布ファイルと同じ信頼できない経路から期待値を取得するだけでは、その経路の改ざんを検出できません。

## 公開前に必要な検証

- 正常な署名・attestationを持つ各成果物が、期待するworkflow・コミットに対して検証できる。
- 未署名、無効な署名、期限切れや失効等で信頼できない証明書、タイムスタンプなし、想定外の発行者を拒否する。テスト用証明書は一般公開の信頼された署名の代わりにしない。
- アーカイブまたは`SHA256SUMS`の改変、別コミット・別workflowによるattestation、未登録ファイルの追加、アーカイブ外へのパスを拒否する。
- 署名・attestationの生成失敗、検証ツールや信頼情報の取得失敗、権限不足の場合に公開されない。必要なGitHub承認や保護設定を迂回しない。
- 失効確認先へ接続できない場合と、ドラフトから再取得した資産が検証済みのハッシュと一致しない場合に公開されない。公開後の不一致時に配布停止・通知が行える。
- 展開後の配布物が必要なファイルを含み、各対応Windows環境で動く。GUIは実画面で操作し、署名検証と動作確認を別々に記録する。
- 実際のリリース資産を利用者の手順で取得し、READMEまたはリリースノートに掲載する手順で、アーカイブとハッシュ一覧の真正性、およびWindows実行ファイルの発行者を検証できる。

実在する証明書・署名基盤・リリース資産・CI実行結果を確認できるまでは、これらを完了としません。公開資産の形式ごとの検証実装と、署名鍵を使う権限の決定が次の実装作業です。

### attestationとハッシュ一覧の個別検証

`support_tools/python/upd_commander_checker/scripts/verify_release_attestations.py` は、信頼できる別経路で得たworkflow・ソースコミット・署名workflowのコミットを明示し、`SHA256SUMS`と各掲載ファイルのattestationおよびSHA-256を検証する補助ツールです。`gh`とGitHubへの接続が必要です。引数は `--workflow` に `tomiya7688/upd-commander-base-design/.github/workflows/<workflow>.yml`、`--source` と `--signer` にそれぞれ完全なコミットSHAを指定します。`SHA256SUMS`は小文字のSHA-256、半角空白2個、パスを含まないファイル名の形式です。

このツールは`SHA256SUMS`とディレクトリ直下のファイル集合を照合し、未記載・不足ファイルやサブディレクトリを拒否します。アーカイブ内部、Authenticode、証明書の失効、公開先の資産は検証しません。同時に書換えられない信頼済みディレクトリで実行し、その前提を `--trust-immutable-directory` で明示してください。このオプションは利用者の確認であり、ディレクトリの変更不能性を自動検証するものではありません。成功後に元のディレクトリのファイルをそのまま公開する根拠にもできません。検証から公開までのバイト列の同一性は別途保証してください。成功しても、公開ゲートの完了や配布の許可を意味しません。実際のattestationを使った検証も、公開用workflowと成果物の用意後に必要です。

`.github/workflows/attestation-integration.yml` はテスト専用のダミーデータにattestationを生成します。このworkflowの識別子を公開成果物の信頼対象として使用しないでください。テスト対象をリリース資産としてアップロードしません。
