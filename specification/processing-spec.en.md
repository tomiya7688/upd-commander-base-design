# Processing Specification

[日本語](processing-spec.md) | **English**

Processing performs the actual work within each layer.

## 1. Common principles

1. Actual calculation, transformation, rendering, and data operations belong in Processing.
2. Processing handles only responsibilities belonging to its own layer.
3. Processing must not directly call Processing in another layer.
4. When information from another layer is required, Processing returns that need to Commander.
5. Processing should be split into single-responsibility units whenever practical.

## 2. UI Processing

UI Processing handles presentation, input interpretation, and UI-specific transformations.

Examples:

- pygame rendering
- generating display strings
- converting HP values into gauge width
- deciding UI-only warning states
- screen-transition effects
- UI interpretation of input

UI Processing must not contain game/business rules or persistence logic.

## 3. Process Processing

Process Processing owns application/game rules and calculations.

Examples:

- battle calculations
- AI decisions
- status updates
- win/loss rules
- experience calculations
- semantic state transitions

Process Processing does not know UI-specific presentation or persistence mechanisms.

## 4. Data Processing

Data Processing owns data retrieval, persistence, and the transformations required for those operations.

Examples:

- JSON / CSV / DB / binary I/O
- serialization / deserialization
- converting external representation into internal transfer representation
- converting values into persistence representation

Data Processing does not decide what data means to the game/business or how it is displayed.

## 5. Processing results

Processing returns results to Commander. A result may represent:

- completed output
- a value for the next Processing operation
- a request indicating that data from another layer is required
- failure information

Processing must not use Messenger directly as a shortcut to send cross-layer messages.

## 6. Single responsibility

When one Processing unit contains multiple independent operations, split it.

Instead of one catch-all `BattleProcessing`, for example, use responsibility-oriented units such as:

```text
DamageProcessing
StatusEffectProcessing
TurnOrderProcessing
VictoryProcessing
```

The responsibility boundary is not tied to a specific language construct such as class/module/function, but it must remain traceable.
