# Messenger Specification

[日本語](messenger-spec.md) | **English**

Messenger is the component dedicated to cross-layer communication in UPD Commander.

## 1. Core principles

1. Messenger carries cross-layer requests and responses.
2. Messenger must not contain real processing.
3. Messenger forwards received content to the target layer's Messenger or Commander.
4. Messenger must not make game/business, UI-presentation, or persistence-rule decisions.
5. Only the minimum transformation required to establish communication is allowed.
6. When multiple arguments or return values exist, they should normally be grouped by Compresser into a single communication unit rather than interpreted individually by Messenger.
7. Messenger must not interpret the business meaning of fields inside a payload produced by Compresser.

## 2. What Messenger may do

- send and receive messages
- identify destinations
- attach or interpret command types required for routing
- manage communication identifiers such as `request_id` / `correlation_id`
- synchronous or asynchronous transmission
- detect communication failures
- perform minimal wrapping/unwrapping required by the communication contract
- carry a communication unit produced by Compresser

## 3. What Messenger must not do

- damage calculation
- UI presentation decisions
- gauge-ratio calculation
- semantic data formatting
- save-data generation
- game/business state updates
- Data Processing such as query construction
- act as a substitute for Process Processing
- inspect payload fields and make business decisions based on them

## 4. Formal communication route

```text
UI Messenger <-> Process Messenger <-> Data Messenger
```

Direct UI Messenger <-> Data Messenger communication is prohibited.

## 5. Direction examples

Request:

```text
UI Commander
  -> UI Messenger
  -> Process Messenger
  -> Process Commander
```

Data request:

```text
Process Commander
  -> Process Messenger
  -> Data Messenger
  -> Data Commander
```

Response:

```text
Data Commander
  -> Data Messenger
  -> Process Messenger
  -> Process Commander
```

```text
Process Commander
  -> Process Messenger
  -> UI Messenger
  -> UI Commander
```

## 6. Message content

Messages should use framework-independent values and avoid leaking layer-specific implementation objects.

Avoid sending objects such as `pygame.Event`, DB connections, or file handles across layer boundaries.

Prefer explicit contracts such as:

```text
{
  command: "load_character",
  payload: {
    character_id: "abc123"
  }
}
```

Even when multiple values are required, prefer one communication unit conforming to the Message Contract rather than expanding Messenger argument lists.

Packaging/unpacking responsibilities follow `compresser-spec.en.md`.

## 7. Communication mechanism

UPD Commander does not prescribe a transport. Implementations may use direct calls, event/message queues, signals, callbacks, async/await, IPC, or network communication. Responsibility boundaries remain the same regardless of transport.
