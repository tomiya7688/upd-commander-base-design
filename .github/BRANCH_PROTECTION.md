# main branch protection

通常の変更はPull Request経由で `main` に入れ、`Checker Self Check / required` の成功を必須にする。

## 推奨設定

GitHub の `Settings -> Branches` または Rulesets で `main` を対象にし、以下を設定する。

- Pull Request を必須にする
- required status check に `Checker Self Check / required` を指定する
- branch更新後はrequired checkを再実行する設定を有効にする
- 通常の直接pushは禁止する
- 管理者bypassは、CI自体が壊れて修復不能な緊急時だけに限定する

`Checker Self Check / required` は Python / Go / C++ / C# / license packaging / Windows GUI の全jobが成功した場合だけ成功する集約gateである。

Workflowは全Pull Requestで起動する。path filterを付けると、required checkが生成されないPRが発生するため付けない。

---

# English

Normal changes should enter `main` through pull requests and require `Checker Self Check / required` to succeed.

Recommended GitHub branch protection / ruleset settings:

- require pull requests before merging
- require the `Checker Self Check / required` status check
- require checks again after the branch is updated
- block normal direct pushes to `main`
- reserve administrator bypass for emergency CI recovery only

`Checker Self Check / required` is an aggregate gate. It succeeds only when the Python, Go, C++, C#, license-packaging, and Windows GUI jobs all succeed.

The workflow intentionally runs on every pull request. Do not add path filters, because a required check that is not created can leave a pull request permanently blocked or allow inconsistent enforcement depending on repository settings.
