# Testing Rules

[日本語](testing-rules.md) | **English**

This document defines testing rules for implementations following UPD Commander.

## 1. Core principles

1. Each layer should be independently testable as far as practical.
2. Processing should normally be unit-testable without starting Commander or Messenger.
3. Commander tests verify correct routing/orchestration, not the correctness of real processing.
4. Messenger tests verify delivery and contract integrity, not business results.
5. Cross-layer integration tests verify that the formal communication route is used.

## 2. UI Layer

Presentation conversion and presentation-state decisions should be separable from rendering where practical.

Examples:

- HP 24/100 -> ratio 0.24
- ratio below threshold -> `warning=true`

Rendering tests requiring a UI framework may be separated from pure presentation-conversion tests.

## 3. Process Layer

Process Processing is the primary target for unit tests. Cover calculations, state transitions, boundary values, invalid input, and requests for additional data. Tests should not require actual UI or Data implementations.

## 4. Data Layer

Test Data Processing for I/O formats and conversions. Tests using real files/DBs may be separated from tests using memory, temporary storage, or mocks.

## 5. Commander tests

Verify, for example:

- command A invokes Processing A
- Messenger is invoked when Data is needed
- the correct Processing resumes after Data returns
- Processing results are passed to the correct Messenger
- Commander itself does not transform values

## 6. Messenger tests

Verify:

- correct destination
- message-contract preservation
- request/response correlation
- communication-failure detection

Do not test game/business logic correctness through Messenger tests.

## 7. Architecture tests

Where supported by the language/toolchain, use static analysis or tests to detect forbidden dependencies such as:

- UI importing Data
- Processing importing another layer's Processing
- Messenger directly calling Processing
- UI-framework dependencies leaking into Process
- DB-driver dependencies leaking into UI

## 8. Regression tests

Refactoring and responsibility movement must preserve externally visible command/response contracts. When a contract change is necessary, treat it as an explicit Message Contract change.
