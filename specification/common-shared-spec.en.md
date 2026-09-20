# Common / Shared Specification

[日本語](common-shared-spec.md) | **English**

This document defines responsibilities, dependency direction, and configuration for UPD Commander `Common` / `Shared` areas.

Common / Shared is not a fourth layer beside UI / Process / Data. It is a **layer-neutral asset area** for resources that do not own a layer-specific responsibility.

## 1. Core model

An Application remains a three-layer structure:

```text
Application
├─ UI
├─ Process
└─ Data
```

A neutral shared area may exist when needed:

```text
Application
├─ UI
├─ Process
├─ Data
└─ Common
```

or `Shared`. This does not turn UPD into a four-layer architecture.

## 2. Eligible assets

Common / Shared may contain layer-neutral assets such as:

- DTOs
- Messages / Contracts
- value objects
- enums and constants
- immutable data structures
- pure type definitions shared by multiple layers
- inter-Application contracts
- small framework-independent utilities
- serialization/protocol schemas

A shared utility must not own side effects or UI / Process / Data responsibilities.

## 3. Prohibited assets

Do not move these into Common / Shared merely to bypass layer rules:

- UI rendering or event handling
- business/game logic
- DB/file/network persistence
- UI / Process / Data Processing
- layer-specific Commanders or Messengers
- framework-coupled implementations
- processing that is actually used by only one layer and merely relocated

The directory name itself does not exempt code from responsibility rules.

## 4. Dependency direction

| Source | Target | Allowed | Notes |
|---|---|---:|---|
| UI | Common/Shared | Yes | neutral contracts/values |
| Process | Common/Shared | Yes | neutral contracts/values |
| Data | Common/Shared | Yes | neutral contracts/values |
| Common/Shared | Common/Shared | Yes | neutral-to-neutral |
| Common/Shared | UI | No | loses neutrality |
| Common/Shared | Process | No | depends on business/game processing |
| Common/Shared | Data | No | depends on persistence details |

Common / Shared does not replace the normal cross-layer communication path.

## 5. Application scope

An Application-local root such as:

```text
applications/main/common/
```

belongs to the `main` Application. Another Application must not directly depend on it unless it is an explicitly defined inter-Application contract.

A product-level root such as:

```text
common/
shared/
```

may contain product-wide neutral contracts, DTOs, Messages, value objects, protocols, and schemas.

Product-level Common must also remain independent of UI / Process / Data implementations.

## 6. Checker classification

A checker classifies files below a recognized common root as `Common`.

`Common` is not classified as UI, Process, or Data.

Therefore:

- a source under Common must not receive an unassigned-layer error merely because it is outside UI / Process / Data
- Common must not be treated as a required fourth layer
- Common -> UI/Process/Data references are Core Rule violations
- UI/Process/Data -> Common references are allowed unless another boundary rule is violated

## 7. Path classification priority

Resolve the innermost Application scope first, then the innermost recognized layer/common root within that scope.

Root matching uses complete directory-component equality. A filename containing the word `common` is not a Common root.

## 8. Config contract

`config/path.json` may contain:

```json
{
  "common_roots": ["common", "shared"]
}
```

Default:

```text
common_roots = ["common", "shared"]
```

Validation:

- must be an array
- every item must be a non-empty string
- items must not contain path separators
- `.` and `..` are invalid
- values equal to recognized UI / Process / Data layer names are invalid
- case comparison follows normal platform path rules
- duplicates may be normalized
- an empty array disables automatic Common/Shared recognition

Meaning must be identical in Python, Go, C++, and C#.

## 9. Compatibility

When `common_roots` is omitted, existing configurations use the default `["common", "shared"]`.

Existing ignore, `.updcommanderignore`, `enabled_rules`, and CLI path override semantics are unchanged.

## 10. Exceptions

Do not allow Common -> layer-specific implementation merely for convenience. Any unavoidable exception follows the normal documented-exception policy and should cause reconsideration of whether the asset belongs in Common.

## 11. Non-goals

This specification does not determine:

- whether a Common asset is actually referenced by multiple layers
- the Warning for an effectively single-layer Common asset
- language-specific reference extraction
- package/namespace/module naming

The single-layer Common warning is defined separately by the #83 series.

## 12. Decision table

| Scenario | Expected |
|---|---|
| UI -> Common DTO | allowed |
| Process -> Common value object | allowed |
| Data -> Common schema | allowed |
| Common contract -> Common value object | allowed |
| Common -> UI rendering implementation | Core Rule violation |
| Common -> Process business processing | Core Rule violation |
| Common -> Data repository implementation | Core Rule violation |
| Application-local Common used as another Application's internal implementation | cross-Application violation |
| Product-level Common contract used by multiple Applications | allowed |
| source under recognized Common root | not an unassigned-layer error |
