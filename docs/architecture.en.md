# Architecture

[日本語](architecture.md) | **English**

## 1. Purpose

UPD Commander Base Design separates UI, processing, and data responsibilities so that execution paths are easy to trace.

The target system is divided into three layers:

- **UI Layer**: input handling, presentation decisions, rendering, and other UI concerns
- **Process Layer**: game logic and application-level computation
- **Data Layer**: data retrieval, persistence, loading, and formatting

## 2. Common structure

Each layer generally contains three kinds of responsibility.

### Commander

Decides **what should be called next** inside the layer.

A Commander does not perform real work such as calculation, rendering, persistence, or transformation.

### Messenger

Handles communication with adjacent layers.

A Messenger does not make game/business or presentation decisions.

### Processing

Performs the actual work belonging to the layer.

Examples:

- UI Processing: formatting for display, rendering, UI interpretation of input
- Process Processing: damage calculation, turn handling, rule evaluation
- Data Processing: loading, saving, conversion, formatting

## 3. Dependencies

The basic communication path is:

```text
UI ↔ Process ↔ Data
```

Direct layer skipping is prohibited:

```text
UI → Data
Data → UI
```

Processing in one layer must not directly call Processing in another layer.

```text
UI Processing → Process Processing  # NG
Process Processing → Data Processing # NG
```

Cross-layer communication goes through Messenger.

## 4. UI presentation responsibility

The UI layer decides **how** values returned by the Process layer are displayed.

If Process returns:

```text
hp = 24
max_hp = 100
```

Process does not decide:

- HP gauge width
- whether text should be `24 / 100`
- whether danger should be shown in red
- whether the value should blink

Those decisions belong to UI Processing.

Accordingly, UPD Commander Base Design does not require names such as ViewModel from other architectures to become formal structural concepts.

## 5. Design goals

- confine UI framework dependencies to the UI layer
- make the Process layer easy to unit-test
- isolate storage format and storage destination changes from game/business logic
- keep call paths predictable
- prevent Commander and Messenger bloat
- make migration to other languages or engines easier
