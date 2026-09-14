# Recommended Practices

[日本語](recommended-practices.md) | **English**

This document defines recommended implementation practices for UPD Commander Design. They are not universal mandatory rules, but should normally be followed to preserve readability, maintainability, loose coupling, and testability.

## 1. One file, one responsibility

A file should normally contain one responsibility.

In many languages, a file/module is also a practical unit of naming, visibility, public surface, dependency management, and access control. UPD therefore treats the file boundary as a fundamental responsibility boundary.

Split files that contain multiple independent responsibilities. When behavior can no longer be explained by the file name, responsibility overload is likely.

In particular, avoid mixing Commander, Messenger, and Processing responsibilities in the same file.

## 2. One module, one responsibility

A module means an independent implementation unit with one responsibility. Its concrete representation depends on the language.

Typical examples:

```text
Go      -> package / file / struct + functions
Python  -> .py module / class / function group
C++     -> .cpp + .hpp/.h / namespace / class
C#      -> .cs file / namespace / class
C       -> .c + .h
Rust    -> module / struct + impl
Functional languages -> module / function group
```

### Go

Package is a larger public/dependency boundary, but responsibilities should still be split into files inside the package. `struct + methods` is optional; stateless work may be expressed by functions.

### Python

A `.py` file is naturally a module. A class may be used when useful, but simple responsibilities do not need to be class-based.

### C++

A `.cpp` plus `.hpp/.h` pair can represent one implementation module. When classes are used, align one class with one responsibility and make file/responsibility boundaries correspond where practical. Namespace/free-function/internal-linkage designs are equally compatible with UPD.

### C#

Although classes are a common implementation unit, define the file/module responsibility first. Normally place one major class per file and align that class with the file responsibility. Supporting data-only types may exist where they truly belong to the same responsibility, subject to the checker placement rules.

## 2.5. One class, one responsibility

In languages that use classes, one major class should normally have one responsibility.

A class that changes for multiple independent reasons is overloaded. UPD does not require multiple responsibilities to be packed together merely because processing volume grows: Commander can sequence multiple Processing units or lower-level Commanders.

Under strict UPD, unusually large classes/modules are therefore strong signals of insufficient responsibility splitting.

The checker uses mechanical warnings as indicators:

- `UPD401`: a responsibility unit is too large
- `UPD402`: multiple major responsibility-bearing types share one file

Data-only helper types such as DTOs/records/enums are handled separately by placement rules. Justified performance/generated-code exceptions may use Ignore with a reason; the checker itself aims to pass strict Self Check without responsibility warnings.

## 3. One function, one action

A function should normally perform one action.

Avoid mixing independent actions such as retrieval, validation, calculation, persistence, rendering, and notification in one function. Split them and let Commander or an upper processing unit sequence them.

Do not over-split tiny inseparable steps that together form one clear action.

## 4. Comments at processing boundaries

Comments should explain purpose, boundaries, and intent rather than restating syntax.

Good:

```python
# Validate the input.
validate_input(data)

# Calculate the battle result.
result = calculate_battle(data)

# Return the result to the caller.
return result
```

Avoid comments that merely paraphrase the call itself.

If a block is hard to understand without many comments, consider splitting the function or responsibility.

## 5. Processing modules under Commander should normally be closed

A Processing module directly controlled by a Commander should normally behave as a closed responsibility unit:

- called by its parent Commander
- not directly called from Processing under another Commander
- does not directly call Processing under another Commander
- does not directly call another layer's Processing
- returns control to its parent Commander when cross-layer communication is required
- normally returns control to the parent Commander when another independent operation must be selected

Preferred flow:

```text
Commander
  ↓
Processing A
  ↓
return result to Commander
  ↓
Commander
  ↓
Processing B
```

Avoid hidden horizontal chains such as:

```text
Commander
  ↓
Processing A
  ↓
Processing B
  ↓
Processing C owned by another Commander
```

The purpose is to prevent Processing-to-Processing coupling from creating execution paths that bypass Commander.

## 6. Exceptions for helper operations

A closed Processing module may call helper operations such as:

- pure functions supporting the same responsibility
- private/internal operations of the same module
- value objects and data structures
- side-effect-free shared transformations
- utilities explicitly defined as common project components

If a helper begins to perform independent business/game decisions, persistence, UI updates, or cross-layer access, return control to the parent Commander instead.

## 7. Position of classes in OOP languages

UPD Commander is not inherently object-oriented.

Classes are implementation mechanisms inside modules. Design priority is:

```text
file / module boundary
  ↓
responsibility / dependency / access-control boundary
  ↓
class or other internal structure when useful
```

Multiple classes in one file are not categorically forbidden, but independent responsibilities should not be hidden inside one file.

## 8. Recommended review questions

During review, check at least whether:

- one file contains multiple independent responsibilities
- one module contains multiple independent responsibilities
- one class contains multiple independent responsibilities
- a class/module is becoming unnecessarily large
- a function performs multiple independent actions
- processing boundaries and intent are readable
- Processing modules are directly coupled sideways
- call paths bypass Commander
- use of classes has blurred file/module boundaries
- exceptions have explicit reasons and limited scope
