# Data Flow

[日本語](data-flow.md) | **English**

## From UI input to processing

```text
User Input
  ↓
UI Commander
  ↓
UI Processing (UI-side input interpretation when needed)
  ↓
UI Messenger
  ↓
Process Messenger
  ↓
Process Commander
  ↓
Process Processing
```

## When Process needs Data

```text
Process Processing
  ↓ request required data
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

Data Processing performs retrieval, persistence, and required formatting, then returns the result through Data Messenger.

```text
Data Processing
  ↓
Data Commander
  ↓
Data Messenger
  ↓
Process Messenger
  ↓
Process Commander
  ↓
Process Processing
```

## Returning calculated results

After Process Processing finishes, results are sent to UI and/or Data according to their purpose.

### Return to UI

```text
Process Processing
  ↓
Process Commander
  ↓
Process Messenger
  ↓
UI Messenger
  ↓
UI Commander
  ↓
UI Processing
  ↓
Display
```

UI Processing may convert Process results into presentation values.

```text
Process result:
  hp = 24
  max_hp = 100

UI Processing:
  text = "24 / 100"
  gauge_ratio = 0.24
  warning = true
```

### Return to Data

When a result must be persisted:

```text
Process Processing
  ↓
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

## Principles

- Messenger only communicates.
- Commander only selects and directs processing.
- Process Processing performs calculations based on semantic meaning.
- UI Processing decides how values are presented.
- Data Processing decides how values are stored and restored.
