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

現在:

- Python: ASTベースチェッカー、短出力、Ignore、Application境界、EXEビルド対応
- Go: go/astベースチェッカー、短出力、Ignore、Application境界、単体EXEビルド対応
- C++: 軽量静的チェッカー、短出力、Ignore、Application境界、CMake/EXEビルド対応
- C#: 軽量静的チェッカー、短出力、Ignore、Application境界、単一EXE publish対応
