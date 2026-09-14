# Implementation Quality Requirements

[日本語](implementation-quality.md) | **English**

This document defines the minimum quality conditions for implementations adopting UPD Commander Design.

These conditions are not responsibility-separation rules themselves, but they are baseline acceptance requirements for an implementation considered compliant and of normal quality.

## 1. Indentation

- Follow the standard indentation convention of the language/project.
- Keep nesting visually distinguishable.
- Do not mix indentation styles inside one file.
- Prefer an available formatter over manual formatting.

## 2. Syntax and buildability

- Code must not contain syntax errors.
- Compiled languages must pass their normal build.
- Interpreted languages must pass the normal syntax/import/startup checks used by the project.
- Do not leave unresolved references or code that immediately fails during normal execution.

## 3. CI compliance

- When a project has normal CI, changes must pass it.
- CI failure must not be accepted as the standard state.
- Depending on the project/toolchain, CI should include appropriate checks such as formatter/format check, linter, unit tests, import/compile checks, build checks, and architecture/dependency checks.
- Do not disable tests or static analysis merely to make CI green.

## 4. Formatter / Linter

- Follow the project formatter when one is defined.
- Do not normally leave linter warnings/errors unresolved.
- When suppression is necessary, minimize its scope and record the reason in code or configuration.

## 5. Tests

- Do not consider a change complete while existing tests are broken.
- When a specification change makes an existing test obsolete, update the implementation and the test together.
- Add tests for important new behavior when practical.

## 6. Warnings

- Persistent build/lint/test/runtime warnings should normally be resolved.
- When a warning is intentionally retained, document its reason and impact scope.

## 7. Position in the specification

These are not merely optional design recommendations; they are acceptance conditions for a normal-quality implementation.

Specific commands and tools may vary according to each project's language, toolchain, and CI environment.
