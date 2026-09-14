# main branch protection

通常の変更はPull Request経由で `main` に入れ、次の2つのstatus checkを必須にする。

- `CI / required`
- `Checker Self Check / required`

## 推奨設定

GitHub の `Settings -> Branches` または Rulesets で `main` を対象にし、以下を設定する。

- Pull Request を必須にする
- required status check に `CI / required` と `Checker Self Check / required` を指定する
- branch更新後はrequired checkを再実行する設定を有効にする
- 通常の直接pushは禁止する
- 管理者bypassは、CI自体が壊れて修復不能な緊急時だけに限定する

`CI / required` は通常の品質gateである。Pythonのcompile/test、Goのgofmt/go vet/test、C++のconfigure/build/test、C#のCSharpier/build/testがすべて成功した場合だけ成功する。

`Checker Self Check / required` はUpd checker固有の自己検証gateである。Python / Go / C++ / C# の checker self check、license packaging、Windows GUI acceptanceがすべて成功した場合だけ成功する。C# jobではCSharpierも再確認するため、既存のrequired設定だけが有効な期間でもCSharpier違反はmerge gateから漏れない。

Workflowは全Pull Requestで起動する。path filterを付けると、required checkが生成されないPRが発生するため付けない。

## CSharpier

CSharpierはrepository-local .NET toolとして `.config/dotnet-tools.json` に固定する。

ローカルではrepository rootで以下を実行する。

```text
dotnet tool restore
dotnet csharpier format support_tools/cs
dotnet csharpier check support_tools/cs
```

CIでは `check` のみ実行し、整形されていないコードを自動修正せず失敗として扱う。

---

# English

Normal changes should enter `main` through pull requests and require both status checks:

- `CI / required`
- `Checker Self Check / required`

Recommended GitHub branch protection / ruleset settings:

- require pull requests before merging
- require both `CI / required` and `Checker Self Check / required`
- require checks again after the branch is updated
- block normal direct pushes to `main`
- reserve administrator bypass for emergency CI recovery only

`CI / required` is the ordinary quality gate. It requires Python compile/tests, Go gofmt/go vet/tests, C++ configure/build/tests, and C# CSharpier/build/tests to pass.

`Checker Self Check / required` is the Upd-checker-specific verification gate. It requires the Python, Go, C++, and C# self checks plus license packaging and Windows GUI acceptance. The C# self-check job also verifies CSharpier so formatting cannot bypass the existing required gate during migration.

Both workflows intentionally run on every pull request. Do not add path filters, because a required check that is not created can leave a pull request permanently blocked or cause inconsistent enforcement.

CSharpier is pinned as a repository-local .NET tool in `.config/dotnet-tools.json`. From the repository root run `dotnet tool restore`, then `dotnet csharpier format support_tools/cs` to format or `dotnet csharpier check support_tools/cs` to verify without modifying files.
