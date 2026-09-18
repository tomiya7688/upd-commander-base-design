# Flat Layer Checker Rules

[日本語](flat-layer-check-rules.md) | **English**

This document defines the Attention emitted when a UI / Process / Data layer is large while most source files remain directly under the layer root.

This rule is not a UPD conformance requirement. Small flat layouts and intentionally flat layouts for performance, build, or compatibility reasons remain valid.

## 1. Rule

- Rule ID: `UPD405`
- Severity: `attention`
- Purpose: suggest navigability improvements when a large layer keeps most source files directly at its root
- Default CI behavior: non-blocking

Example diagnostic:

```text
UPD405 large flat layer reduces navigability; consider grouping related responsibilities
```

The rule does not prescribe directory names, nesting depth, or Application decomposition.

## 2. Scope

Each recognized Application / Sub Application evaluates its UI, Process, and Data layer roots independently.

Files classified into another Application / Sub Application are not counted in the parent Application, even if they are physically below it.

When an Application boundary cannot be identified, evaluate each recognized UI / Process / Data root within the current classification root.

## 3. Eligible source files

Count files that the language checker normally treats as source files.

Exclude files that:

- are path-ignored by existing CLI/config/`.updcommanderignore` behavior
- contain one of these default excluded directory names in their path: `generated`, `third_party`, `vendor`, `external`, `build`
- belong to a different Application / Sub Application

Directory-name comparison follows the platform's normal path comparison rules.

## 4. Size and flatness

Split eligible files into:

- `direct_files`: files whose parent directory is the layer root itself
- `nested_files`: files below a subdirectory of the layer root

`total_files = direct_files + nested_files`.

Defaults:

```text
flat_layer_min_files = 12
flat_layer_min_direct_percent = 80
```

Emit UPD405 only when both conditions hold:

```text
total_files >= flat_layer_min_files
direct_files * 100 >= total_files * flat_layer_min_direct_percent
```

The integer comparison above is normative; implementations must not depend on floating-point rounding.

With defaults, 11 files never trigger. For 12 files, 9 direct / 3 nested does not trigger, while 10 direct / 2 nested and 12 direct / 0 nested do.

## 5. Config contract

`config/path.json` may contain:

```json
{
  "flat_layer_min_files": 12,
  "flat_layer_min_direct_percent": 80
}
```

`flat_layer_min_files` must be a positive integer. `flat_layer_min_direct_percent` must be an integer from 1 through 100. Missing fields use the defaults. Zero, negative/out-of-range values, booleans, strings, fractions, and `null` are config errors.

The meaning must be identical in Python, Go, C++, and C#.

## 6. Finding location

Emit at most one UPD405 finding per layer root.

- path: layer-root path relative to the scan root
- line: `1`
- severity: `attention`

Do not duplicate the finding on individual source files.

## 7. Common fixture contract

[flat-layer-fixtures.json](flat-layer-fixtures.json) is the normative numeric fixture contract.

Required cases are small-flat, large-nested, large-flat, generated-heavy, boundary-below-percent, and boundary-over-percent. Language-specific fixtures may differ in syntax only; file counts, directory placement, and expected severity must match.

## 8. Non-goals

UPD405 does not decide whether individual files/classes are oversized, whether namespace/package names are appropriate, whether a particular folder name is used, whether an Application split is required, or whether a flat layout violates UPD.

Those concerns remain with existing responsibility and boundary rules.
