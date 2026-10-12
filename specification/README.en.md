# UPD Commander Technical Specification

[日本語](README.md) | **English**

This index is the entry point for checking implementation rules for UPD Commander. For the design concepts and their rationale, start with [Architecture](../docs/architecture.en.md). This page points to each rule's authoritative document instead of repeating rule summaries.

## Reading paths

### First-time readers

1. [Architecture](../docs/architecture.en.md) — overview of UI / Process / Data and their roles
2. [Layer Specification](layer-spec.en.md) — responsibilities of the three layers
3. [Commander Specification](commander-spec.en.md) — how Commanders coordinate calls
4. [Messenger Specification](messenger-spec.en.md) — routes for cross-layer communication
5. [Guideline Levels (Japanese)](guideline-levels.md) — the distinction between mandatory, recommended, and practical guidance

These five documents provide the basic structure and rule-strength model. Consult the authoritative document below when applying a specific rule.

### Checker implementers and maintainers

1. [Guideline Levels (Japanese)](guideline-levels.md) — contract for classifying rule strength
2. [Checker Severity Boundary (Japanese)](checker-severity.md) — mapping between design rules and Checker severity
3. [Checker Regression Matrix](checker-regression-matrix.en.md) — shared regression expectations across Checkers
4. The relevant rule-contract document — inputs, decisions, and outputs for the rule being implemented
5. [Implementation Quality Requirements](implementation-quality.en.md) — implementation and CI quality conditions

### Looking up a rule

Choose a topic from the list below. Checker users can start with “Rule and Checker contracts”; implementers can start with “Structure and communication” or “Rule and Checker contracts.”

## Specification documents

### Structure and communication

- [Layer Specification](layer-spec.en.md) — responsibilities of UI / Process / Data
- [Application Boundary](application-boundary.en.md) — Application boundaries and recursive use of the three-layer structure
- [Common / Shared Specification](common-shared-spec.en.md) — responsibilities and dependency direction of the layer-neutral shared area
- [Commander Specification](commander-spec.en.md) — Commander responsibilities and prohibitions
- [Messenger Specification](messenger-spec.en.md) — cross-layer communication responsibilities and formal routes
- [Processing Specification](processing-spec.en.md) — actual processing responsibilities in each layer
- [Dependency Rules](dependency-rules.en.md) — allowed and prohibited dependencies
- [Data Commander Communication](data-commander-communication.en.md) — warning conditions for communication between Data Commanders
- [Message Contract](message-contract.en.md) — contracts for cross-layer messages
- [Error Handling](error-handling.en.md) — error detection, propagation, and conversion

### Rule strength and Checker decisions

- [Guideline Levels (Japanese)](guideline-levels.md) — classification into mandatory, recommended, and practical guidance
- [Checker Severity Boundary (Japanese)](checker-severity.md) — meanings and mapping of Danger / Warning / Attention
- [Responsibility Check Rules](responsibility-check-rules.en.md) — responsibility units and UPD401–UPD404 contracts
- [Flat Layer Checker Rules](flat-layer-check-rules.en.md) — UPD405 contract for large, flat layers
- [Model Attention Checker Rules](model-attention-check-rules.en.md) — UPD406 contract for value groups
- [Compresser / Container Checker Rules](compresser-check-rules.en.md) — Containerization decision contract
- [Finding Baseline Contract](baseline-contract.en.md) — Finding fingerprints and baseline JSON contract
- [Checker Regression Matrix](checker-regression-matrix.en.md) — shared Checker regression expectations

### Recommended design and general quality

- [Compresser Specification](compresser-spec.en.md) — recommended design for grouping values into Containers
- [Recommended Practices](recommended-practices.en.md) — recommendations for responsibility boundaries, placement, and examples
- [Testing Rules](testing-rules.en.md) — test categories and design rules
- [Implementation Quality Requirements](implementation-quality.en.md) — general quality requirements for builds, CI, formatters, and linters

## Interpreting the specifications

Each document is authoritative for the rule strength and scope it defines. See [Guideline Levels (Japanese)](guideline-levels.md) for mandatory / recommended / practical classification, and [Checker Severity Boundary (Japanese)](checker-severity.md) for its mapping to Checker severity.

This index defines neither duplicate rule summaries nor a separate decision order. Consult the relevant specification when implementing a rule, and record project-specific reasons when choosing not to follow a recommendation.
