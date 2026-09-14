# Commander Specification

[日本語](commander-spec.md) | **English**

Commander is the component responsible for **directing calls** in UPD Commander.

## 1. Core principles

1. Commander must not implement real processing.
2. According to a received request or result, Commander calls the appropriate Processing or Messenger.
3. Commander acts as the control point inside its layer.
4. Commander must not directly call Processing in another layer.
5. Commander itself must not perform retrieval, calculation, rendering, or formatting.

## 2. What Commander may do

- distinguish command types
- select which Processing to call
- pass required input to Processing
- receive Processing results
- decide which Processing runs next
- call Messenger when another layer is needed
- route returned cross-layer results to the appropriate Processing
- branch on flow states such as success, failure, or continuation

## 3. What Commander must not do

Examples of forbidden work include:

- calculations such as `damage = attack - defense`
- HP-ratio calculation
- JSON formatting
- file reads
- SQL execution
- pygame rendering
- string decoration
- game/business rule evaluation
- constructing persistence-specific data structures

These belong to Processing in the corresponding layer.

## 4. Branching in Commander

Commander may branch on **which processing should run**, but not on the contents of the processing itself.

Allowed:

```text
command = attack
  -> call Attack Processing

command = save
  -> send Save Request to Messenger
```

Not allowed:

```text
if attack > defense:
    damage = attack - defense
else:
    damage = 1
```

The latter is game logic and belongs to Process Processing.

## 5. When Processing needs more data

Processing does not directly send requests to another layer.

Recommended flow:

```text
Processing
  -> return "additional data required" to Commander
Commander
  -> give data request to Messenger
Messenger
  -> send it to the target layer
```

On response:

```text
Messenger
  -> Commander
Commander
  -> Processing that should continue
```

## 6. Detecting Commander bloat

If the following accumulate inside Commander, suspect a responsibility violation:

- calculation expressions
- complex loops
- data transformations
- UI-framework calls
- file/DB API calls
- conditions that encode business rules

The problem is not line count by itself; the problem is real processing leaking into Commander.

## 7. State in Commander

Commander may retain the minimum state needed to continue orchestration, for example:

- an ID identifying which request a response belongs to
- information identifying which Processing should resume
- management information for pending asynchronous responses

Commander must not become the owner of game/business state or UI state itself.
