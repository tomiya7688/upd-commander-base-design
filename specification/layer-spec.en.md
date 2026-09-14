# Layer Specification

[日本語](layer-spec.md) | **English**

This document defines the responsibilities of the three UPD Commander layers: `UI`, `Process`, and `Data`.

## 1. Common rules

1. A system must normally be separated into `UI Layer`, `Process Layer`, and `Data Layer`.
2. Each layer consists of a `Commander`, required `Messenger` components, and Processing modules that perform real work.
3. Processing in one layer must not directly call Processing in another layer.
4. Cross-layer requests and responses must pass through Messenger.
5. Commander and Messenger must not contain real processing.
6. Framework-specific dependencies must remain inside the layer that needs them.

## 2. UI Layer

The UI Layer is responsible for interaction with the user and presentation-related work.

### 2.1 UI responsibilities

- acquiring keyboard, mouse, gamepad, touch, and similar input
- creating windows, screens, and UI components
- rendering
- animation, screen transitions, and presentation effects
- converting values returned by Process into presentation values or states
- decisions meaningful only to presentation
- UI-framework-specific processing

Examples include converting `hp=24, max_hp=100` into `24 / 100`, calculating gauge width, enabling a warning below a threshold, or handling pygame `Surface`, `Rect`, and `Event` objects.

### 2.2 UI must not own

- damage or experience calculations
- AI decisions
- game/business win/loss rules
- direct save-data I/O
- direct database, file, or persistence access

### 2.3 UI-specific types

UI-framework-specific types should not cross the UI boundary. Before sending data to Process, convert it into framework-independent values or messages suitable for UI Messenger.

## 3. Process Layer

The Process Layer owns the application's semantic rules, decisions, and calculations.

### 3.1 Process responsibilities

- game/business logic
- numerical calculations
- state transitions
- rule evaluation
- data processing independent of UI and storage formats
- calculations using data returned from the Data Layer

### 3.2 Process must not own

- UI-framework rendering
- UI-specific decisions such as display text or color
- Data-specific knowledge such as file paths or DB connection mechanisms
- serialization tied to a persistence format

### 3.3 When more data is needed

Process Processing must not call Data directly. It returns the need for additional data to Process Commander. Process Commander sends the request through Process Messenger, and returned data comes back through Process Messenger and Process Commander to the appropriate Processing.

## 4. Data Layer

The Data Layer owns retrieval, persistence, and transformations required for storage/retrieval.

### 4.1 Data responsibilities

- file and database I/O
- external storage access
- retrieval and persistence
- serialization / deserialization
- converting storage representation into internal transfer representation
- formatting required during retrieval or persistence

### 4.2 Data must not own

- UI presentation decisions
- game/business calculations
- UI event handling
- Process-layer business decisions

The Data Layer does not need to know what a value means to the game or business.

## 5. Layer boundaries

The formal boundaries are:

```text
UI <-> Process <-> Data
```

Direct `UI <-> Data` communication is prohibited. Even when UI wants to save something, UI requests it through Process, which determines the semantic meaning of the save before Data performs persistence.

## 6. Response direction

Responses obey the same boundaries as requests:

```text
Data -> Data Messenger -> Process Messenger -> Process Commander -> Process Processing
Process -> Process Messenger -> UI Messenger -> UI Commander -> UI Processing
```

Data must not directly return results to UI.
