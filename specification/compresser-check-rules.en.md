# Compresser / Container Checker Rules

[日本語](compresser-check-rules.md) | **English**

This document defines severity and decision policy for static checks based on `compresser-spec.en.md`.

## 1. Containerization is not universally mandatory

Grouping the inputs of one class into one Input Container and its return information into one Output Container is recommended, but it is not a mandatory rule for all projects.

Container/Package creation, storage, extraction, and serialization have their own implementation and runtime costs. Therefore, multiple arguments or return values alone must not be treated as an error.

## 2. Attention

When an individual operation has multiple inputs or multiple return values and Containerization may improve readability, emit `attention`:

```text
A UPD301 ... multiple inputs reduce readability; consider one Input Container
A UPD302 ... multiple return values reduce readability; consider one Output Container
```

Attention indicates a design-improvement opportunity, not a compliance failure. When the benefit is small or packaging cost is larger, the current implementation may be retained after review.

## 3. Warning

For Commander or Messenger, emit a `warning` only when introducing Compresser/Container is expected to substantially reduce the component itself:

```text
W UPD303 ... Compresser/Container introduction is expected to substantially reduce this Commander/Messenger
```

The purpose is not to punish multiple arguments. It is to detect Commander/Messenger bloat caused by repeatedly relaying many communication values.

The initial heuristic treats the opportunity as substantial when estimated reducible size is at least 10 lines and roughly 20% or more of the effective code size of the Commander/Messenger.

These numbers are checker heuristics, not the design philosophy itself, and may be adjusted using false-positive/false-negative experience.

## 4. Estimating reducible size

Depending on parser quality, estimate reduction from:

- excess input values
- excess return values
- multi-line method/function signatures
- effective code size of the target class or file

AST-based implementations should prefer class/type-level size. Lightweight or regex-based implementations may use the UPD component file as an approximation.

## 5. Compresser granularity

A Compresser may be defined per class or per related feature. The goal is not to minimize the number of Compressers, but to preserve one-class/one-Container boundaries while keeping responsibilities readable.

## 6. The checker itself

UPD Commander Checker is treated as a design example and follows stricter self-compliance than normal user projects.

The checker implementation should Containerize multiple inputs/returns when applicable and aim for no remaining Attention/Warning findings in strict Self Check.

This self-rule does not make Containerization mandatory for general projects.
