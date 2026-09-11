# Anti-patterns

UPD Commander Base Design では、責務境界を壊す次の構造を避けます。

## Commander に実処理を書く

```python
class BattleCommander:
    def attack(self, attacker, target):
        damage = attacker.attack - target.defense
        target.hp -= max(damage, 0)
```

Commander がダメージ計算を行っているためNGです。

Commander は処理を呼ぶだけにします。

```python
class BattleCommander:
    def attack(self, attacker, target):
        return battle_processing.attack(attacker, target)
```

## Messenger に条件分岐を書く

```python
if hp < max_hp * 0.25:
    show_warning = True
```

これは UI 表示上の判断なので UI Processing の責務です。

Messenger は内容を判断せず配送します。

## UI から Data へ直接アクセスする

```text
UI → Data
```

禁止します。

必要なデータは Process を通して要求します。

```text
UI → Process → Data
```

## Process が表示方法を決める

```text
HPが25%以下なので文字色を赤にする
```

赤色や点滅などは UI Layer の責務です。

Process Layer は意味上の値や状態を返します。

```text
hp = 24
max_hp = 100
```

## Data がゲームルールを判断する

Data Layer は保存形式や取得方法を扱いますが、ゲーム上の意味は判断しません。

```text
このモンスターは毒無効なので毒状態を保存しない
```

このような判断は Process Layer が担当します。

## 他層の Processing を直接呼ぶ

```text
UI Processing → Process Processing
Process Processing → Data Processing
```

層をまたぐ場合は Messenger を使用します。

## Commander / Messenger の肥大化

Commander や Messenger のコード量が増え続ける場合、それ自体が責務漏れの兆候です。

次を確認します。

- 計算が混ざっていないか
- 変換処理が混ざっていないか
- UI表現判断が混ざっていないか
- データ加工が混ざっていないか
- 1ファイル1責務を破っていないか

該当する処理は適切な Processing へ分離します。
