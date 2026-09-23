# Common / Shared Usage Checker Rules

**日本語正本**

本書は、Common / Shared と分類された資産が UI / Process / Data のどの層から参照されているかを集計し、単一層専用資産を検出するための共通契約を定義する。

## Rule Contract

- Rule ID: `UPD407`
- Severity: Warning
- Message: `UPD407 Common/Shared asset is referenced by only one layer; consider moving it into that layer`
- Finding location: the Common / Shared source file

## Asset Identity

An asset is one source file. Its identity is the normalized path relative to the scan root, including its Application scope when applicable. Normalize path separators to `/` and apply the platform's ordinary path case rules. Different files remain different assets even when their basenames match. Declarations within one file share the file's identity; this file-level unit is used by all four language implementations.

Only assets under roots recognized as Common / Shared by the existing configuration and classification rules are candidates. UI, Process, and Data assets are not candidates.

## Reference Layer Set

For each candidate asset, collect the set of distinct source layers (`UI`, `Process`, `Data`) from which the checker can resolve a source reference to that asset. Count layers, not reference sites, files, projects, or applications. Multiple references from one layer contribute one member to the set.

References are resolved within the applicable Application scope under the existing Application boundary rules. Product-level Common / Shared may be referenced across Applications; each referring source still contributes its classified layer. Application-local Common / Shared is considered only against valid references in its owning Application scope.

The set is derived from source references the checker can resolve statically. Language-specific import, include, or symbol resolution may differ, but the meaning of each result must remain the same. An unresolved reference does not add a layer.

## Result Classification

| Distinct referring layers | Result |
|---|---|
| Exactly one | Emit one `UPD407` Warning for the asset |
| Two or three | No `UPD407` finding |
| None (unused asset) | No `UPD407` finding; unused assets are outside this rule |

Do not emit more than one finding for the same asset identity. A reference from Common / Shared itself, generated or test code, or a public API declaration does not count as a UI / Process / Data referring layer for this rule.

## Exclusions

- Generated source is excluded using the checker's existing generated-source exclusion behavior.
- Test-only references do not contribute to the layer set.
- Public API/export declarations alone are not references and do not contribute to the layer set.
- References whose source cannot be classified as UI, Process, or Data do not contribute.
- Unused Common / Shared assets are not reported by `UPD407`.

## Ignore and Configuration

`UPD407` follows the existing rule selection and ignore contracts: disabling the rule suppresses its findings; path/rule and supported inline ignores suppress only `UPD407` findings in their configured scope. Common roots continue to come from the existing Common / Shared configuration. No new configuration key is introduced by this rule.

## Message Stability

All language implementations use rule code `UPD407`, Warning severity, and the message text specified above. A finding identifies the asset path. Layer names may be appended as stable diagnostic metadata, but must not change the rule's decision or severity.
