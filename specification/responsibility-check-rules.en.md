# Responsibility Check Rules

[日本語](responsibility-check-rules.md) | **English**

This document defines the common criteria used by UPD Commander Checker to detect excessive responsibility and responsibility-unit placement problems.

## UPD401: Responsibility unit too large

`UPD401` is a `warning` indicating that a responsibility unit has become large enough to be a strong split candidate.

It does **not** claim that every unit over 250 lines necessarily contains multiple responsibilities. UPD emphasizes early visibility of oversized responsibility units, so this is a conservative warning boundary.

### Common thresholds

- source span: **up to 250 lines**
- responsibility-bearing methods: **up to 12 methods**
- **251+ lines** or **13+ methods** -> `UPD401` warning

Exceeding either boundary is sufficient.

### Responsibility units

When AST analysis can identify types, prefer type-level units over whole-file size.

- Python: class
- Go: named type + receiver methods aggregated as one unit
- C++: class / struct
- C#: type declaration such as class / struct / record

Only when a language/source cannot determine a type-level responsibility unit may file/module size be used as an approximation. The finding should make the approximation clear.

### Counting lines

When AST start/end lines are available, use the responsibility unit's source span. Do not change thresholds per language by separately removing comments/blank lines.

For Go, where methods are declared outside the type declaration, aggregate the named-type declaration span and the spans of methods whose receiver is that type.

### Counting methods

Count normal methods directly owned by the responsibility unit.

- constructors are excluded
- properties/accessors are excluded from method count
- methods inside nested types are not added to the outer type

Only languages without a normal method concept may use the nearest equivalent callable member.

## UPD402: Multiple responsibility-bearing types in one file

`UPD402` is a `warning` when one file contains multiple responsibility-bearing types/classes.

Types used only for data storage and containing no business behavior do not count as major responsibility types for `UPD402`; their placement is handled separately by `UPD403/UPD404`.

## UPD403: Data-only type colocated with another type

When a data-only type shares a file with another type, emit `attention`.

The other type may itself be data-only or responsibility-bearing. Both of these are therefore covered:

- data-only type + data-only type
- data-only type + behavioral type/class

A data-only type includes DTOs, Contracts, Value Containers, configuration-holder types, and similar types whose primary purpose is data storage and that do not contain business processing.

The purpose is discoverability: when looking for a type named `Foo`, the recommended structure makes the corresponding file predictable.

A dedicated file containing only one data-only type is not an `UPD403` violation.

## UPD404: Colocated data-only type referenced from another file

When a data-only type that qualifies for `UPD403` is actually referenced from another file, promote the finding to a `warning` and report `UPD404`.

Do not emit `UPD404` merely because a type is `public`, exported, `internal`, etc. The checker must confirm an actual reference using AST or equivalent syntax analysis from another file.

The goal is to make definition locations easy to trace from usage sites. Externally referenced data-only types should normally live in their own file corresponding to the type name.

### Duplicate type names

These rules must not prohibit duplicate type names by themselves.

In particular, identical private/internal/package-local names in separate files may be valid. `UPD403/UPD404` concern **co-location inside one file and actual references from another file**, not global name uniqueness.

### Priority

- one data-only type in its own file: no finding
- colocated data-only type, not referenced elsewhere: `UPD403` attention
- colocated data-only type, referenced elsewhere: `UPD404` warning
- when `UPD404` applies to a type, duplicate `UPD403` output for the same type is unnecessary

## Relationship with UPD401

- `UPD401`: one responsibility unit is too large
- `UPD402`: multiple major responsibility-bearing types share one file
- `UPD403/UPD404`: placement and discoverability of data-only types

Different rules may be reported together when they describe distinct problems in the same file.

## Checker implementation requirements

All language implementations must use the common 250-line / 12-method thresholds defined here.

`UPD403/UPD404` must be based on actual syntactic references, not merely visibility modifiers.

When thresholds or placement semantics change, update this specification first and synchronize all language implementations and boundary tests in the same change.
