# Dependency Rules

[日本語](dependency-rules.md) | **English**

This document defines allowed and prohibited dependencies in UPD Commander.

## 1. Basic dependencies

The only allowed cross-layer boundaries are:

```text
UI <-> Process
Process <-> Data
```

Direct `UI <-> Data` dependency is prohibited.

## 2. Allowed dependency table

| Source | Target | Allowed | Condition |
|---|---|---:|---|
| UI Commander | UI Processing | Yes | call within the same layer |
| UI Commander | UI Messenger | Yes | send a request to Process |
| UI Processing | UI Commander | Yes | return result/additional request |
| UI Messenger | Process Messenger | Yes | cross-layer communication |
| Process Messenger | Process Commander | Yes | deliver received request |
| Process Commander | Process Processing | Yes | call within the same layer |
| Process Commander | Process Messenger | Yes | send to UI/Data |
| Process Processing | Process Commander | Yes | return result/additional request |
| Process Messenger | Data Messenger | Yes | cross-layer communication to Data |
| Data Messenger | Data Commander | Yes | deliver received request |
| Data Commander | Data Processing | Yes | call within the same layer |
| Data Commander | Data Messenger | Yes | return to Process |
| Data Processing | Data Commander | Yes | return processing result |

## 3. Prohibited dependencies

| Source | Target | Reason |
|---|---|---|
| UI Layer | Data Layer | skips Process |
| UI Processing | Process Processing | bypasses Messenger/Commander |
| UI Processing | Data Processing | skips two boundaries |
| Process Processing | Data Processing | bypasses Commander/Messenger |
| Data Processing | Process Processing | leaks Data concerns into game/business flow |
| Data Layer | UI Layer | makes Data depend on presentation |
| Messenger | any Processing | bypasses Commander |
| Commander in one layer | Processing in another layer | bypasses Messenger |

## 4. Import / reference rules

When a language has `import`, `include`, `using`, or equivalent references, dependency rules apply to those source-level references as well.

For example, importing a Data-layer class from UI is prohibited even if the imported symbol is not ultimately called.

Common DTOs, Message Contracts, and value objects may be placed in a Common / Shared area that does not belong to a specific layer. `UI/Process/Data -> Common/Shared` dependencies are allowed, while `Common/Shared -> UI/Process/Data` dependencies are prohibited. Common / Shared is not a fourth layer. Do not use such an area to hide game/business processing, rendering, or data operations. See [Common / Shared Specification](common-shared-spec.en.md) for the normative contract.

## 5. External library dependencies

External dependencies should be confined to the layer that needs them.

Examples:

- pygame -> UI
- DB driver -> Data
- pure calculation library for game/business rules -> Process

Avoid leaking external-library types across layer boundaries.

## 6. Exceptions

Do not create direct-dependency exceptions merely for implementation convenience.

When an exception is unavoidable, document at least:

- why the normal route cannot be used
- dependency scope
- alternatives considered
- whether the exception can be removed later

Temporary exceptions should be tracked as technical debt.
