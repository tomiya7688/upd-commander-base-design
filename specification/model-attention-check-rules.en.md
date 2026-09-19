# Model Attention Checker Rules

[日本語](model-attention-check-rules.md) | **English**

This document defines the Attention emitted when the same related value group is repeatedly listed and may benefit from being represented as a Model, DTO, struct, record, or equivalent semantic unit.

The rule does not require modelization. SoA, SIMD, contiguous layouts, FFI, low-level APIs, and hot paths may intentionally keep values separate for performance or ABI reasons and can be suppressed with the existing rule-specific ignore mechanism.

## 1. Rule

- Rule ID: `UPD406`
- Severity: `attention`
- Purpose: suggest a semantic data unit when the same group of at least three values appears at least twice
- Default CI behavior: non-blocking

Example:

```text
UPD406 repeated value group may benefit from a Model/DTO; items=id,name,email occurrences=2 kind=parameters
```

## 2. Eligible occurrences

A value-group occurrence is one of:

- **parameter group**: effective function/method inputs, excluding implicit receivers such as Python `self`/`cls` and C# extension receivers; variadic/params counts as one item
- **tuple / multi-value group**: tuple literals/returns, destructuring/deconstruction, Go multi-value return/assignment, or equivalent C++ tuple/structured-binding constructs
- **parallel collection access group**: at least three collections referenced with the same index/iterator in one statement/expression

Declarations of several collections alone are not evidence.

UPD406 differs from UPD301: a large parameter count alone is insufficient; the same group must repeat.

## 3. Item keys and signatures

Item keys are derived conservatively:

- identifier -> identifier name
- member access -> terminal member name
- indexed collection -> collection identifier
- parameter -> parameter name

Normalization only lowercases and removes leading underscores. Do not infer synonyms, singular/plural forms, camel/snake equivalence, or types.

The signature is:

```text
(application, layer, occurrence_kind, ordered_item_keys)
```

Order, occurrence kind, Application/Sub Application, and layer are significant. If no Application/layer can be identified, use the current scan/classification scope.

Do not search arbitrary subsets of a larger group.

## 4. Trigger thresholds

Defaults:

```text
model_group_min_items = 3
model_group_min_occurrences = 2
```

Emit one UPD406 finding per signature only when both thresholds are met. A repeated pair of two values does not trigger by default.

The finding location is the first eligible occurrence.

## 5. Existing aggregate exclusion

Do not count as evidence:

- field/member listings inside an existing user-defined Model/DTO/struct/record/dataclass/named-tuple definition
- a parameter/return/argument that is already one user-defined aggregate value
- constructor/initializer sites whose only purpose is populating an existing aggregate

Raw value groups inside methods of an aggregate type may still be eligible.

## 6. Performance and low-level code

Do not guess performance intent from names. For intentional SoA/SIMD/ABI/interop layouts, use the existing rule-specific ignore mechanism, for example:

```text
UPD406 process/hot_path/**
UPD406 data/soa/**
```

Where inline ignores are supported, `upd: ignore UPD406` may suppress an occurrence.

Rule-specific suppression must suppress UPD406 only, not remove the path from all rule analysis.

Existing generated/vendor/path exclusions remain excluded from the occurrence population.

## 7. Config contract

`config/path.json` may contain:

```json
{
  "model_group_min_items": 3,
  "model_group_min_occurrences": 2
}
```

`model_group_min_items` must be an integer >= 3. `model_group_min_occurrences` must be an integer >= 2. Invalid types, fractions, booleans, nulls, and values below the minimum are config errors.

The meaning must be identical in Python, Go, C++, and C#.

## 8. Finding evidence

Each finding must include at least:

- occurrence `kind`
- ordered `items`
- eligible `occurrences` count

Path and line point to the first eligible occurrence. Severity is `attention`. Implementations with structured metadata may retain all additional occurrence locations.

## 9. Common fixture contract

[model-attention-fixtures.json](model-attention-fixtures.json) is normative.

Required cases cover repeated two-item groups, repeated parameter groups, one-off groups, tuples, parallel-index groups, existing aggregates, performance-suppressed code, order differences, and cross-layer-only repetition.

Language fixtures may differ in syntax only; signatures and expected severity must match.

## 10. Non-goals

UPD406 does not determine business semantics, choose a type name, decide AoS versus SoA performance, replace UPD301/UPD302, or make modelization a UPD conformance requirement. It is a conservative review heuristic.
