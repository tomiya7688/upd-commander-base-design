# Commander & Messenger

[日本語](commander-messenger.md) | **English**

## Commander

Commander coordinates calls within a layer.

### What Commander does

- receives requests from Messenger
- calls the appropriate Processing
- asks Messenger to send a request to another layer when necessary
- passes Processing results to the appropriate next operation

### What Commander does not do

- damage calculation
- generation of display strings
- file I/O
- data transformation
- complex rule evaluation

If these responsibilities appear in Commander, move them into Processing.

## Messenger

Messenger handles cross-layer communication.

### What Messenger does

- sends commands or data to an adjacent layer
- receives commands or data from an adjacent layer
- passes received content to its own layer's Commander

### What Messenger does not do

- game/business rule decisions
- UI presentation decisions
- data formatting
- persistence
- choosing which Processing should run

## Call direction

A typical request flows as follows:

```text
UI Commander
  ↓
UI Messenger
  ↓
Process Messenger
  ↓
Process Commander
  ↓
Process Processing
```

When data is required:

```text
Process Commander
  ↓
Process Messenger
  ↓
Data Messenger
  ↓
Data Commander
  ↓
Data Processing
```

Responses travel back through Messenger in the opposite direction.

## Commander / Processing boundary

The distinction is simple:

> **Which processing to call** belongs to Commander.
> **How that processing is performed** belongs to Processing.

Keeping this boundary makes Commander thin.
