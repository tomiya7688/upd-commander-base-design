# UPD Commander Base Design

[日本語](README.md) | **English**

UPD Commander Base Design is an architectural design that separates an application into three layers — **UI / Process / Data** — and separates call orchestration into **Commander**, cross-layer communication into **Messenger**, and actual work into Processing modules owned by each layer.

The goal is not to map names from existing patterns such as MVVM directly onto code structure. The goal is to make the responsibilities and communication paths of UI, application/game processing, and data processing explicit, and to control dependency direction.

## Core idea

A major characteristic of UPD Commander Base Design is that, when the rules are followed, **the responsibilities of classes, modules, files, and access-control units naturally become smaller**.

Even when the amount of processing grows, it does not need to accumulate inside one Commander or processing module. Work can be split into multiple Processing modules or lower-level Commanders, while an upper Commander only calls them in the appropriate order.

For example, a large operation such as `load -> validate -> transform -> save` can be split into four independent operations. The upper Commander is then responsible only for directing that sequence.

Therefore, under a strict UPD design, **large code volume should not by itself force a class or module to become large**. Processing volume can be divided, while coordination remains one responsibility: directing calls.

As a result, a sufficiently large class or module becomes a strong signal of **too many responsibilities or insufficient responsibility splitting**, rather than merely a large amount of code.

UPD therefore aims to keep responsibilities small through structure instead of relying only on developer discipline, and to make oversized responsibility units easier to discover.

## Basic principles

1. Separate the system into `UI`, `Process`, and `Data` layers.
2. A Commander does not perform real work; it calls the appropriate work.
3. A Messenger handles cross-layer communication and does not contain business or game processing.
4. Actual calculation, conversion, rendering, and data operations belong to Processing modules in the appropriate layer.
5. Direct access that skips layer boundaries is prohibited in principle.
6. The Process layer does not know how UI is rendered.
7. The UI layer does not directly access the Data layer.
8. The Data layer does not know UI concerns or game/business logic.

## Basic structure

```text
UI Layer
├─ Commander
├─ Messenger
└─ UI Processing

Process Layer
├─ Commander
├─ Messenger
└─ Process Processing

Data Layer
├─ Commander
├─ Messenger
└─ Data Processing
```

## Typical flow

```text
UI Input
  ↓
UI Commander
  ↓
UI Processing / UI Messenger
  ↓
Process Messenger
  ↓
Process Commander
  ↓
Process Processing
  ↓
Data Messenger (when needed)
  ↓
Data Commander
  ↓
Data Processing
  ↓
Data Messenger
  ↓
Process Messenger
  ↓
Process Commander
  ↓
Process Processing
  ↓
Process Messenger
  ├─→ UI Messenger → UI Commander → UI Processing
  └─→ Data Messenger → Data Commander → Data Processing
```

## Documentation

- [Architecture](docs/architecture.en.md) — overall structure and responsibilities
- [Layer Rules](docs/layer-rules.en.md) — rules for UI / Process / Data
- [Commander & Messenger](docs/commander-messenger.en.md) — responsibilities of Commander and Messenger
- [Data Flow](docs/data-flow.en.md) — request and response flow
- [Anti-patterns](docs/anti-patterns.en.md) — prohibited or discouraged structures
- [Technical Specification](specification/README.en.md) — normative implementation rules

## Scope

UPD Commander Base Design is a project for architectural rules and checker/support tools that help validate those rules.

It is not itself a system for context reduction, AI read routing, exploration stopping, or Context Pack generation.

Other development-support or AI tools may use UPD responsibility boundaries, but those concerns remain separate from UPD Commander itself.

## Most important rule

> **A Commander does not perform processing. A Commander calls the appropriate processing. A Messenger carries communication across layers, and actual work is performed by Processing modules owned by each layer.**

If a Commander or Messenger grows because it has absorbed real processing, that processing should be moved into an appropriate Processing module.
