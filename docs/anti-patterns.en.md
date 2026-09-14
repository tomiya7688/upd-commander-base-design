# Anti-patterns

[日本語](anti-patterns.md) | **English**

UPD Commander Base Design avoids structures that break responsibility boundaries.

## Putting real processing in Commander

```python
class BattleCommander:
    def attack(self, attacker, target):
        damage = attacker.attack - target.defense
        target.hp -= max(damage, 0)
```

This is invalid because Commander performs damage calculation.

Commander should only direct processing:

```python
class BattleCommander:
    def attack(self, attacker, target):
        return battle_processing.attack(attacker, target)
```

## Putting business/presentation branches in Messenger

```python
if hp < max_hp * 0.25:
    show_warning = True
```

This is a UI presentation decision and belongs to UI Processing.

Messenger should deliver content without interpreting its business meaning.

## Direct UI-to-Data access

```text
UI → Data
```

This is prohibited. Required data is requested through Process:

```text
UI → Process → Data
```

## Process deciding presentation

```text
HP is below 25%, so make the text red
```

Color, blinking, and similar presentation belong to the UI Layer.

Process should return semantic values such as:

```text
hp = 24
max_hp = 100
```

## Data deciding game/business rules

The Data Layer owns storage and retrieval, not semantic rules.

```text
Do not save poison because this monster is immune to poison
```

Such a decision belongs to the Process Layer.

## Calling another layer's Processing directly

```text
UI Processing → Process Processing
Process Processing → Data Processing
```

Use Messenger when crossing a layer boundary.

## Bloated Commander / Messenger

Continued growth in Commander or Messenger is a sign that processing responsibilities may have leaked into them.

Check for:

- calculations
- transformations
- UI presentation decisions
- data manipulation
- violations of one-file/one-responsibility

Move such work into the appropriate Processing module.
