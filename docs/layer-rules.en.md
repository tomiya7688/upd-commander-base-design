# Layer Rules

[日本語](layer-rules.md) | **English**

## UI Layer

The UI Layer may depend on UI frameworks.

### Main responsibilities

- handling keyboard, mouse, gamepad, touch, and similar input
- screen transitions
- rendering
- converting Process-layer results into display forms
- managing UI-only state

### Prohibited responsibilities

- calculating game/business rules themselves
- directly accessing the Data Layer
- directly calling Process-layer Processing

## Process Layer

The Process Layer handles the semantic behavior of the application or game.

### Main responsibilities

- game/business logic
- numerical calculations
- rule evaluation
- state transitions
- requesting required data
- producing processing results

### Prohibited responsibilities

- dependency on UI frameworks such as pygame
- presentation decisions such as colors, coordinates, and fonts
- direct access to Data-layer Processing

## Data Layer

The Data Layer handles data retrieval, persistence, and representation conversion.

### Main responsibilities

- file I/O
- database access
- configuration loading
- serialization / deserialization
- converting storage formats into usable transfer formats

### Prohibited responsibilities

- UI presentation decisions
- game/business rule decisions
- calling Process-layer Processing

## Common rules

- Prefer one responsibility per file.
- Prefer one action per function.
- Do not put real processing in Commander.
- Do not put real processing in Messenger.
- Use Messenger for cross-layer communication.
- When Processing grows, split it by responsibility.
