# Message Contract

[日本語](message-contract.md) | **English**

This document defines the minimum contract for messages exchanged between Messengers.

## 1. Core principles

1. A message must not leak the sender layer's internal implementation into the receiver.
2. A message explicitly contains the information the receiver needs to make processing decisions.
3. Implementation-dependent objects such as UI-framework objects, DB connections, or file handles must not be passed across layers.
4. Message structure is defined independently of programming language and transport.
5. Multiple inputs/outputs may be grouped into one communication unit by Compresser when appropriate.
6. Commander/Messenger should normally not interpret the business meaning of individual payload fields.

## 2. Recommended fields

Use as needed:

```text
command       : requested operation
request_id    : request identifier
payload       : request/response values
status        : success / failure / pending, etc.
action        : semantic operation required next
error         : failure information
```

Not every field is mandatory in every message.

## 3. Command

`command` expresses what is being requested, for example `move_player`, `attack`, `load_character`, or `save_game`. Messenger must not perform the business meaning of the command itself.

## 4. Payload

Payload contains the minimum information needed across the boundary.

Good:

```text
{
  character_id: "abc123"
}
```

Bad:

```text
{
  pygame_event: <pygame.Event>,
  db_connection: <Connection>
}
```

When payload creation/unpacking is dedicated to a component, follow `compresser-spec.en.md`.

Except for routing needs, Commander/Messenger must not inspect payload fields and make semantic decisions from them.

## 5. Request / response matching

For asynchronous or parallel requests, use an identifier such as `request_id` so every response can be associated with its request.

## 6. Additional requests from Processing

Processing may return a semantic indication that more cross-layer data is needed:

```text
status: pending
action: request_data
payload:
  command: load_character
  character_id: abc123
```

Commander interprets the flow-level action and invokes Messenger. The Message Contract must not be used as a shortcut for Processing to call Messenger directly.

## 7. Response

Responses may explicitly carry success/failure state.

```text
status: success
request_id: 123
payload:
  hp: 100
  attack: 20
```

```text
status: failure
request_id: 123
error:
  code: data_not_found
  detail: character data not found
```

Multiple return values may be packaged into one response unit by Compresser.

## 8. Shared types

DTOs, structs, records, or equivalent types are recommended for expressing contracts, but those types must not contain UI / Process / Data processing logic.
