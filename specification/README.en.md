# UPD Commander Technical Specification

[日本語](README.md) | **English**

This directory contains the detailed technical rules of UPD Commander Design.

While `docs/` explains the design and its concepts, `specification/` defines the rules implementations are expected to follow.

## Specification documents

- [Layer Specification](layer-spec.en.md) — responsibilities of UI / Process / Data
- [Application Boundary](application-boundary.en.md) — Application/Sub Application boundaries and recursive three-layer structure
- [Commander Specification](commander-spec.en.md) — Commander responsibilities and prohibitions
- [Messenger Specification](messenger-spec.en.md) — cross-layer communication responsibilities and formal routes
- [Compresser Specification](compresser-spec.en.md) — grouping multiple arguments/returns into class-specific Containers
- [Compresser / Container Checker Rules](compresser-check-rules.en.md) — Attention/Warning rules for Containerization and Self Check
- [Responsibility Check Rules](responsibility-check-rules.en.md) — common responsibility-unit rules and UPD401–UPD404
- [Flat Layer Checker Rules](flat-layer-check-rules.en.md) — UPD405 Attention contract for large, predominantly flat UI / Process / Data layers
- [Processing Specification](processing-spec.en.md) — actual processing responsibilities in each layer
- [Dependency Rules](dependency-rules.en.md) — allowed and prohibited dependencies
- [Data Commander Communication](data-commander-communication.en.md) — warning rules for direct Data Commander-to-Commander communication
- [Message Contract](message-contract.en.md) — cross-layer message contracts
- [Error Handling](error-handling.en.md) — error detection, propagation, and conversion
- [Testing Rules](testing-rules.en.md) — unit, integration, and architecture testing rules
- [Recommended Practices](recommended-practices.en.md) — one file/one responsibility, one module/one responsibility, one function/one action, comments, closed processing modules, etc.
- [Implementation Quality Requirements](implementation-quality.en.md) — minimum quality requirements for indentation, build, CI, formatter, linter, and tests
- [Checker Regression Matrix](checker-regression-matrix.en.md) — common regression expectations for all checker implementations

## Top-level rules

1. A system should normally be separated into UI / Process / Data.
2. An independently closed functional boundary is treated as an Application, and UI / Process / Data is recursively applied inside each Application.
3. Do not directly depend on the internals of another Application; use an explicit boundary such as Messenger, Contract, or an upper Commander.
4. Commander does not perform processing; it calls the appropriate Processing or Messenger.
5. Messenger is responsible only for cross-layer communication.
6. When multiple communication values should be grouped, Compresser converts them into a single communication unit. Commander/Messenger should normally not interpret the business meaning inside that unit.
7. Actual calculation, transformation, rendering, and data operations belong to Processing in the corresponding layer.
8. Processing in one layer must not directly call Processing in another layer.
9. UI and Data must not communicate directly.
10. Process does not know UI presentation details or Data storage implementation details.
11. Layer-specific types such as UI-framework objects or DB handles must not leak across boundaries.
12. The fundamental design responsibility unit is a file/module, not a language-specific class.
13. In class-based languages, classes may be used as implementation mechanisms inside a module, but must not blur the responsibility boundary of the file/module.

## Strength of rules

Terms such as **must**, **must not**, and **prohibited** are normative unless stated otherwise.

Terms such as **recommended**, **should**, and **may** are adaptable to project-specific constraints, but must not violate the top-level rules.

Containerization is a readability recommendation rather than a universal mandatory rule. Because packaging itself has cost, an individual non-containerized operation is an Attention, and a Warning is emitted only when substantial Commander/Messenger reduction is expected.

`implementation-quality.en.md` defines acceptance conditions for normal-quality implementations, not merely optional advice.

When introducing an exception, document its reason, scope, alternatives, and whether it can be removed later.
