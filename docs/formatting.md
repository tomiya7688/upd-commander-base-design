# 文書・設定ファイルの整形

## 実行場所と前提

リポジトリのルートで実行します。Node.js 22 と npm が必要です。初回または依存関係を更新した後に、`npm ci` でロックファイルに固定された開発用formatterをインストールしてください。

## 対象とコマンド

整形対象はルートの `README.md`、`README.en.md`、`package.json`、`package-lock.json`、`.prettierrc.json`、`.github/workflows/ci.yml`、およびこのファイルです。コード、生成物、依存パッケージ、他の文書は対象に含めません。

- `npm run format` — 対象ファイルを整形します。
- `npm run format:check` — ファイルを変更せず整形状態を確認します。

formatterには Prettier 3.9.10 を使い、`package.json` と `package-lock.json` で版を固定します。設定はリポジトリルートの `.prettierrc.json` に置き、CIでも `npm run format:check` を実行します。
