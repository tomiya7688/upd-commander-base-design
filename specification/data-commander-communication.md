# Data Commander Communication

Data 層の Commander 同士は、原則として直接通信しないことを推奨します。

```text
Data Commander A
  X
Data Commander B
```

Data Commander は、それぞれ自分の Data 側責務の制御を担当します。Commander 同士が直接依存し始めると、Data 層内部に別の制御フローが生まれ、責務境界や呼び出し元が不明確になりやすくなります。

別の Data 責務との連携が必要な場合は、原則として上位の Commander へ結果を返して次の処理を選択するか、Application 境界を越える場合は Messenger / Contract 等の明示的な境界を使用します。

この構造は必ずしも即時の設計破綻を意味しないため、静的チェッカーでは error ではなく warning とします。

## Checker rule

- `UPD103`: Data Commander が別の Data Commander へ直接依存している場合の warning
- 別 Application の内部 Commander への直接依存は、より強い既存規則 `UPD102` を優先します
- 必要な例外は通常の Ignore 機構で明示できます
