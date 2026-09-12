# Support Tools

UPD Commander 設計を補助する言語別ツール群です。

```text
support_tools/
├─ python/
│  └─ upd_commander_checker/
├─ go/
│  └─ upd_commander_checker/
├─ cpp/         # 将来追加
└─ cs/          # 将来追加
```

各言語のツールは独立してパッケージ化し、他言語実装と依存させません。

現在:

- Python: ASTベースチェッカー、短出力、Ignore、Application境界、EXEビルド対応
- Go: go/astベースチェッカー、短出力、Ignore、Application境界、単体EXEビルド対応
