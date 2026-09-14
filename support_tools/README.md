# Support Tools

UPD Commander 設計を補助する言語別ツール群です。

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

共通設定は [`config/path.json`](config-path.md) を使用します。ビルド時に実行ファイル側へ既定設定を自動生成し、入力先・出力先・Ignore・warnings-as-errors を保存できます。

## Compresser / Container 対応

Compresser 仕様に合わせ、チェッカーはクラス相当の公開操作について次を確認します。

- `UPD301`: 1つのクラス操作へ複数の入力引数を直接渡している可能性。原則として1つの Input Container にまとめる。
- `UPD302`: 複数の戻り値を直接返している可能性。原則として1つの Output Container にまとめる。

Compresser 自体の粒度は固定しません。1クラスにつき1つの Compresser を置いても、関連する複数クラスを機能単位の Compresser にまとめても構いません。

ただし、Container の境界はクラス単位です。機能単位の Compresser が複数クラスを扱う場合も、異なるクラスの入力・出力を1つの共通 Container に混在させてはいけません。

複数のクラス専用 Container を1つの Package / Message にまとめて送ることは許可されます。

Python は AST、Go は go/ast を利用して検出します。C++ / C# は軽量チェッカーのためシグネチャベースのヒューリスティック検出です。

現在:

- Python: ASTベースチェッカー、短出力、Ignore、Application境界、Compresser分類、Container境界チェック、EXEビルド対応
- Go: go/astベースチェッカー、短出力、Ignore、Application境界、Container境界チェック、単体EXEビルド対応
- C++: 軽量静的チェッカー、短出力、Ignore、Application境界、Container境界チェック、CMake/EXEビルド対応
- C#: 軽量静的チェッカー、短出力、Ignore、Application境界、Compresser分類、Container境界チェック、単一EXE publish対応
