# Support Tools

UPD Commander 設計を補助する言語別ツール群です。

Checker を AI / Codex / ChatGPT と開発する場合は、最初に [`AI_CONTEXT.md`](AI_CONTEXT.md) を読んでください。`ai-context-reducer` の方針に基づき、変更ruleから必要な source / tests / specification だけへ routing し、必要情報が揃ったら探索を止めます。`ai-context-reducer` 自体は Checker の runtime/build dependency ではありません。

```text
support_tools/
├─ python/
│  └─ upd_commander_checker/
├─ go/
│  └─ upd_commander_checker/
├─ cpp/
│  └─ upd_commander_checker/
└─ cs/
   └─ upd_commander_checker/
```

各言語のツールは独立してパッケージ化し、他言語実装と依存させません。

配布物に含まれる第三者コンポーネントとライセンスは [`THIRD_PARTY_NOTICES.md`](../THIRD_PARTY_NOTICES.md) を参照してください。ビルドスクリプトは該当するライセンス文書を成果物へコピーします。

共通設定は [`config/path.json`](config-path.md) を使用します。ビルド時に実行ファイル側へ既定設定を自動生成し、入力先・出力先・Ignore・warnings-as-errors を保存できます。

## Compresser / Container 対応

Container 化は一般プロジェクトに対する必須規定ではありません。パッケージ化・取り出し・シリアライズ等にもコストがあるため、チェッカーは可読性改善候補として段階的に通知します。

- `A UPD301`: クラス操作に複数入力がある。Input Container 化を検討する Attention。
- `A UPD302`: クラス操作に複数返却値がある。Output Container 化を検討する Attention。
- `W UPD303`: Commander / Messenger で、Compresser / Container 導入によりクラスを大幅に圧縮できると推定された場合の Warning。

`UPD303` は単純な違反件数では出しません。初期実装では、削減可能量が10行以上であり、かつ対象 Commander / Messenger の有効コード量のおおむね20%以上を削減できると推定される場合を「大幅な圧縮」とみなします。言語ごとの解析能力に応じてクラス単位またはファイル単位で近似します。

Compresser 自体の粒度は固定しません。1クラスにつき1つの Compresser を置いても、関連する複数クラスを機能単位の Compresser にまとめても構いません。読みづらくなるほど大きくなった場合は分割します。

ただし、Container の境界はクラス単位です。機能単位の Compresser が複数クラスを扱う場合も、異なるクラスの入力・出力を1つの共通 Container に混在させてはいけません。複数のクラス専用 Container を1つの Package / Message にまとめて送ることは許可されます。

通常実行では Attention は失敗条件ではありません。`--warnings-as-errors` は Warning を失敗扱いにし、`--attentions-as-errors` は Attention も失敗扱いにします。Checker 自身の Self Check では両方を有効にし、推奨している Container 化をツール自身が破らないようにします。

Python は AST、Go は go/ast を利用して検出します。C++ / C# は軽量チェッカーのためシグネチャベースのヒューリスティック検出です。

現在:

- Python: ASTベースチェッカー、短出力、Ignore、Application境界、Compresser分類、Container境界チェック、EXEビルド対応
- Go: go/astベースチェッカー、短出力、Ignore、Application境界、Compresser分類、Container境界チェック、単体EXEビルド対応
- C++: 軽量静的チェッカー、短出力、Ignore、Application境界、Compresser分類、Container境界チェック、CMake/EXEビルド対応
- C#: 軽量静的チェッカー、短出力、Ignore、Application境界、Compresser分類、Container境界チェック、Checker自身のContainer化、単一EXE publish対応
