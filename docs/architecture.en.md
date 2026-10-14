# Architecture

[日本語](architecture.md) | **English**

This document introduces the relationship between UPD's three layers and three roles for first-time readers. It starts with an example of where to place one feature, then explains the overall model. For detailed implementation rules, see the [technical specification index](../specification/README.en.md).

## Example: search for products and show a list

Consider a feature where a user enters a keyword on a search screen, retrieves product information, and displays the results. A **layer** describes the kind of work; a **role** describes an activity within a layer.

| Layer | Commander (selects calls) | Messenger (carries cross-layer requests and results) | Processing (does the layer's work) |
| --- | --- | --- | --- |
| UI | Selects the next UI-side operation for the search action | Sends the keyword to Process and receives the results | Reads the input field and formats returned products for display |
| Process | Routes the search request to validation or data retrieval | Receives the UI request and relays the Data request and result | Validates the keyword and determines the search conditions and application result |
| Data | Selects which data retrieval or storage operation to call | Receives the search request from Process and returns retrieved results | Reads and writes product information in a database or file |

Requests and results pass through Messengers in adjacent layers: UI, Process, and Data. UI does not read the database directly, and Process does not manipulate screen widgets. See [Data Flow](data-flow.en.md) for concrete routes and [Commander & Messenger](commander-messenger.en.md) for the boundary between those roles.

## Layers and roles are separate dimensions

A **layer answers “what kind of work is this?”** A **role answers “what does this part do within the layer?”** The three roles are not additional layers stacked on top of UI, Process, and Data. They describe how work in each layer is organized: call coordination, cross-layer communication, and actual work.

```text
Role / Layer   UI                         Process                    Data
Commander      Selects a UI operation     Selects validation/retrieval Selects read/write operation
Messenger      Sends requests to Process  Relays UI/Data requests      Returns results to Process
Processing     Interprets input/displays  Validates conditions/calcs   Reads/writes data
```

Columns are layers; rows are roles. For product search, formatting a keyword for display is UI Processing, validating search conditions is Process Processing, and reading products from a database is Data Processing. Each layer's Commander selects the next operation, and its Messenger carries cross-layer requests and results.

This table and diagram explain the concepts; they do not require a class or file with the same name for every role in every layer. Physical decomposition can follow the language and project context.

## The three layers

- **UI Layer** handles interaction with the user, including input, screen state, presentation, and rendering.
- **Process Layer** handles application rules, input validation, calculations, and determining results. It does not depend on screen colors or database storage formats.
- **Data Layer** handles retrieving, saving, loading, and converting data from databases, files, or external sources. It does not decide presentation or application rules.

Separating the layers helps, for example, to change a screen or storage destination without also rewriting search or calculation rules.

## The three roles

- A **Commander** selects the next operation within its layer and arranges the sequence. It does not do the actual calculation, rendering, or persistence.
- A **Messenger** carries requests and results between adjacent layers. It does not make application decisions or choose presentation.
- **Processing** performs the actual work belonging to a layer. Examples include input interpretation and display in UI, validation and calculation in Process, and reading and writing in Data.

Normative rules about placement and prohibited dependencies are defined in the [Layer Specification](../specification/layer-spec.en.md), [Commander Specification](../specification/commander-spec.en.md), and [Messenger Specification](../specification/messenger-spec.en.md).

## UI decides how to present results

After Process returns product names and prices, UI decides whether to display a price as “$12.00” and whether to show products as rows or cards. Process does not need to know screen colors, text formatting, or layout. Conversely, UI does not own the search conditions or price-calculation rules.

UPD does not require class names borrowed from a particular design pattern. A project can use names and structures natural to its language or framework as long as it preserves the required roles and communication paths.

## Next steps

- To check layer rules, read [Layer Rules](layer-rules.en.md).
- To understand the boundary between Commander and Messenger, read [Commander & Messenger](commander-messenger.en.md).
- To follow requests and results, read [Data Flow](data-flow.en.md).
