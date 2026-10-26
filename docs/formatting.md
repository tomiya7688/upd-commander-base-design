# 文書・設定ファイルの整形

用語の意味は[日本語用語集](glossary.md)で確認できます。

## 実行場所と前提

リポジトリのルートで実行します。Node.js 22 と npm が必要です。初回または依存関係を更新した後に、`npm ci` でロックファイルに固定された開発用formatterをインストールしてください。

## 対象とコマンド

整形対象はルートの `README.md`、`README.en.md`、`AI_CONTEXT.md`、`THIRD_PARTY_NOTICES.md`、`docs/` と `specification/` 直下の日本語Markdown、`support_tools/` 以下のMarkdown、`.github/BRANCH_PROTECTION.md` です。設定ファイルの `package.json`、`package-lock.json`、`.prettierrc.json`、`.github/workflows/ci.yml`、`.github/workflows/checker-self-check.yml`、`.github/actions/python-trust-setup/action.yml` も対象です。

`.prettierignore` は `*.en.md` の翻訳文書を除外し、従来から確認するルートの `README.en.md` は対象に戻しています。文書中のコード例は `embeddedLanguageFormatting: "off"` で再整形しません。ソースコード、生成物、依存パッケージ、ライセンス原文、個人用の未追跡ファイルは上記コマンドの対象に含めません。

- `npm run format` — 対象ファイルを整形します。
- `npm run format:check` — ファイルを変更せず整形状態を確認します。

formatterには Prettier 3.9.10 を使い、`package.json` と `package-lock.json` で版を固定します。設定はリポジトリルートの `.prettierrc.json` に置き、CIでも `npm run format:check` を実行します。
