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

### 2.1 Target callables

UPD301 / UPD302 apply to callables defined as operations of a type/module, regardless of access visibility.

- private / protected / unexported methods are covered as well as public/exported methods.
- constructors are initialization responsibilities and are excluded from UPD301 / UPD302.
- Python excludes `__init__` and `__new__` as construction hooks, while private-like methods such as `_run` remain covered.
- C# / C++ exclude language-level constructors.
- Go covers receiver methods regardless of visibility and excludes package-level functions. Names such as `NewX` alone are not used to infer constructors.

This scope exists to keep operation-level readability consistent, including inside implementations, rather than only cleaning up public APIs.

### 2.2 Effective input count for UPD301

UPD301 emits `attention` **only when a callable's effective input count exceeds the configured maximum**.

The default maximum is **2 inputs**.

| Effective inputs | Default result |
|---:|---|
| 0 | no finding |
| 1 | no finding |
| 2 | no finding (boundary) |
| 3+ | `UPD301` attention (boundary + 1 or more) |

The goal is to avoid mechanically recommending Containerization for ordinary operations with roughly two inputs.

#### 2.2.1 What counts as one input

The effective input count is the number of independent argument slots in the call signature. It does not change based on value type, size, default values, or the number of runtime elements.

| Form | Effective count |
|---|---:|
| ordinary positional parameter | 1 |
| positional-only parameter | 1 |
| positional-or-keyword parameter | 1 |
| keyword-only parameter | 1 |
| optional/defaulted parameter | 1 |
| variadic parameter / parameter pack | 1 per declaration |
| implicit receiver (`this`, Go receiver, etc.) | 0 |
| Python method receiver `self` / `cls` | 0 |

Additional rules:

- Python `*args` and `**kwargs` each count as one input; runtime element count is irrelevant.
- Go `func (T) Run(a, b int)` has two effective inputs. A variadic `values ...T` counts as one.
- C# `params T[]` counts as one input.
- A C++ function parameter pack counts as one input.
- Constructors are excluded by section 2.1, so constructor parameters are not counted for UPD301.
- Explicit non-receiver parameters count as one slot even when language-specific modifiers such as `ref`, `in`, `out`, or defaults are present.

For Python, the `self` / `cls` exclusion applies to receiver parameters. Static methods have no implicit receiver, so their explicit parameters are counted normally.

### 2.3 Configuration contract

The UPD301 maximum-input setting is named `upd301_max_inputs`.

```json
{
  "upd301_max_inputs": 2
}
```

It means "the maximum effective input count allowed without emitting UPD301."

- omitted: `2`
- valid values: **integers >= 1**
- `effective_inputs <= upd301_max_inputs`: no finding
- `effective_inputs > upd301_max_inputs`: `UPD301` attention
- `null`, boolean, string, fractional number, zero, or negative number: configuration error
- configuration errors follow the existing config-error path: analysis does not start and the process exits non-zero

Existing configuration files remain loadable when the key is absent. Users who want the legacy behavior in which two inputs already trigger UPD301 can set `upd301_max_inputs: 1`.

This setting controls UPD301 only. UPD303 keeps its independent "substantial compression" heuristic and must not directly reuse `upd301_max_inputs` as its trigger threshold. UPD303 may share the effective-input counting semantics.

### 2.4 Local exceptions

When Containerization is intentionally avoided for a performance hot path, ABI/API compatibility, allocation avoidance, or another local constraint, do not add a dedicated UPD301 exception syntax.

Use the existing inline ignore, path/rule ignore, or CI-gate mechanisms and record a reason when possible. Raising the project-wide `upd301_max_inputs` for one exceptional location is not recommended.

### 2.5 Common four-language boundary fixtures

With the default `upd301_max_inputs = 2`, all four implementations must use these expectations:

| Fixture | Effective inputs | Expected |
|---|---:|---|
| no-input | 0 | no finding |
| one-input | 1 | no finding |
| two-inputs | 2 | no finding |
| boundary | 2 | no finding |
| boundary-plus-one | 3 | `A UPD301` |
| one-fixed-plus-variadic | 2 | no finding |
| two-fixed-plus-variadic | 3 | `A UPD301` |
| custom-max-1 / two-inputs | 2 | `A UPD301` |
| custom-max-3 / three-inputs | 3 | no finding |
| custom-max-3 / four-inputs | 4 | `A UPD301` |

Representative boundary code follows. In every language, `Good` has two effective inputs and `Bad` has three.

```python
class Worker:
    def Good(self, left, right):
        pass

    def Bad(self, left, right, mode):
        pass
```

```go
type Worker struct{}

func (Worker) Good(left, right int) {}
func (Worker) Bad(left, right, mode int) {}
```

```cpp
struct Worker {
    void Good(int left, int right) {}
    void Bad(int left, int right, int mode) {}
};
```

```csharp
internal sealed class Worker
{
    internal void Good(int left, int right) { }
    internal void Bad(int left, int right, int mode) { }
}
```

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
