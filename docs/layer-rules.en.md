# Layer Rules

[日本語](layer-rules.md) | **English**

This document explains what UI, Process, and Data are responsible for, while distinguishing mandatory rules, recommended structure, and practical tips. For example, in product search, UI handles input and presentation, Process decides the search conditions, and Data retrieves product information. See [Architecture](architecture.en.md) for the overall model.

## Rule strength

| Category | Meaning | Example |
| --- | --- | --- |
| Mandatory (UPD Core) | Violating it means the design does not meet UPD requirements. | UI does not access Data directly. |
| Recommended | Suggested for maintainability and testability, but not a condition for a design to qualify as UPD. | Aim for one responsibility per file. |
| Practical tip | Helpful for understanding or review; it is not a requirement. | Use a large file as a prompt to review responsibility boundaries. |

The formal meaning of these categories is defined in the Japanese canonical [Guideline Levels](../specification/guideline-levels.md). This page also labels each section so readers can tell how strong its rules are without first consulting another document.

## Mandatory — UPD Core Rules

The following responsibility boundaries are mandatory. Physical file and class decomposition may vary with performance and implementation constraints, but these boundaries remain in force.

### UI Layer

The UI Layer receives input and decides screen state and presentation. It may depend on UI frameworks.

The UI Layer follows these rules:

- It does not own application/game rules or Process-level calculations.
- It does not access the Data Layer directly. It goes through Process when data is needed.
- It does not directly call Processing in another layer, such as Process.
- It does not move presentation decisions such as colors, coordinates, or fonts into Process.

### Process Layer

The Process Layer determines the meaning of results through application rules, search-condition validation, calculations, state transitions, and similar work.

The Process Layer follows these rules:

- It does not depend on UI frameworks or presentation details such as screen colors, coordinates, or fonts.
- It does not depend on Data implementation details such as file formats, databases, or storage destinations.
- It does not directly call Data-layer Processing. When it needs data, it returns the request as a result to its Commander, which asks for communication through Messenger.

### Data Layer

The Data Layer retrieves and persists data and performs the format conversions needed for those operations.

The Data Layer follows these rules:

- It does not decide UI presentation.
- It does not decide application or game rules.
- It does not directly call Process-layer Processing.

### Commander and Messenger

- A Commander selects appropriate operations or communication and organizes their call order. It does not take on independent calculations, rendering, persistence, or other real processing.
- A Messenger carries requests and results across layers. It does not perform application or presentation decisions as part of the communication route.
- Requests and results crossing a layer boundary go through Messenger. Processing in one layer does not directly call Processing in another.

## Recommended — Recommended Structure

The following structure makes responsibilities easier to trace, change, and test. Choosing not to adopt it does not by itself violate a mandatory UPD requirement.

- Give each file and module primarily one responsibility.
- Keep each class's responsibility small when using classes.
- Have each function or method primarily perform one action.
- When processing grows to include several independent tasks, consider splitting it by responsibility.
- Align the logical structure of namespaces, packages, or modules with folders where practical.

If these recommendations conflict with performance, memory, build methods, platform constraints, or existing assets, a project may choose not to adopt them. The mandatory boundaries above still apply.

## Practical tips

- Use comments to explain the purpose or boundary of work, such as “validate input” or “return result,” rather than paraphrasing each line of code.
- When a file or type is large, review it for mixed responsibilities instead of treating line count alone as a violation.
- Read Checker Warnings and Attentions as prompts for design review. Check whether the relevant rule is mandatory or recommended before deciding how to respond.

## Further reading

- Formal responsibilities of each layer → [Layer Specification](../specification/layer-spec.en.md)
- Formal definitions of rule strength (Japanese canonical) → [Guideline Levels](../specification/guideline-levels.md)
- Commander and Messenger roles → [Commander & Messenger](commander-messenger.en.md)
