# Error Handling Specification

[日本語](error-handling.md) | **English**

This document defines error handling and propagation in UPD Commander.

## 1. Core principles

1. Errors are detected in the layer where they occur.
2. Errors that can be resolved inside that layer may be handled by its Processing.
3. Errors requiring caller-level decisions are returned to Commander.
4. Errors that must cross a layer boundary are carried by Messenger.
5. Messenger must not make business decisions based on the meaning of an error.

## 2. UI Layer errors

UI-specific failures remain in UI when no other layer needs to know about them. Examples include rendering-resource failures, UI-component creation failures, and input-device errors.

## 3. Process Layer errors

Game/business failures are interpreted by Process, for example insufficient action conditions, missing semantic targets, insufficient MP, or invalid state transitions.

When UI presentation is required, Process returns semantic failure information and UI decides how it is displayed. Process must not decide dialog style, error color, or similar presentation details.

## 4. Data Layer errors

Data detects retrieval/persistence failures such as missing files, DB connection failures, invalid formats, and read/write errors.

When necessary, Data converts implementation-specific exceptions into failure information defined by the cross-layer contract.

## 5. Propagation

Even a Data-originated error follows the normal communication route:

```text
Data Processing
  -> Data Commander
  -> Data Messenger
  -> Process Messenger
  -> Process Commander
  -> Process Processing / Process decision
  -> Process Commander
  -> Process Messenger
  -> UI Messenger
  -> UI Commander
  -> UI Processing
```

Process may decide what a technical Data failure means to the application or game.

## 6. Exceptions crossing boundaries

Avoid leaking language/library-specific exception objects into another layer. In particular, do not directly pass DB-driver exceptions or UI-framework exceptions across layers.

Convert them to contract-level information when needed:

```text
error:
  code: save_failed
  detail: optional detail
```

## 7. Logging

Logging is separate from error handling itself. A layer may log its own technical details, but logging must not be used as a reason to violate responsibility boundaries.
