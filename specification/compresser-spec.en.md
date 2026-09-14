# Compresser Specification

[日本語](compresser-spec.md) | **English**

Compresser is a dedicated component that groups input and return values passed through Commander/Messenger boundaries into class-specific Containers so they are easier to handle as communication units.

## 1. Purpose

Commander and Messenger are responsible for selecting, calling, and transporting processing, not for understanding the processing contents themselves.

Large argument/return lists increase Commander/Messenger code and force relay components to know communication structure. Compresser reduces this pressure by grouping one class's inputs into one Input Container and its return information into one Output Container.

> As a rule, only the endpoints should need to understand the meaning of values. Intermediate Commander/Messenger components carry Containers as communication units without interpreting their contents.

## 2. Core principles

1. Inputs passed to one class should normally be grouped into one Input Container.
2. Return information from one class should normally be grouped into one Output Container.
3. A single scalar return value does not require an artificial complex Container; multiple return values should be grouped.
4. Compresser implementation granularity is flexible: one per class or one per related feature are both allowed.
5. Even when one Compresser supports multiple classes, the one-class/one-Container boundary must be preserved.
6. Multiple Containers may be grouped into one Package/Message for transport.
7. Do not merge unrelated class/responsibility Containers into one giant shared Container.
8. Commander/Messenger should not interpret the business meaning inside Containers/Packages produced by Compresser.
9. Compresser owns Container creation, packing, and extraction structure.
10. Compresser may handle representation but must not perform business processing, presentation decisions, or persistence decisions.
11. Communication units must not leak layer-specific implementation objects such as UI-framework objects, DB connections, or file handles.
12. Compresser must not move Processing responsibilities into Commander/Messenger.

## 3. Class-specific Containers

Basic form:

```text
ClassA
  Input  -> ClassAInputContainer
  Output -> ClassAOutputContainer
```

Instead of:

```text
ClassA(user_id, user_name, age, address, token, option, retry_count)
```

prefer:

```text
input = ClassAInputContainer(...)
ClassA(input)
```

For multiple return values:

```text
return ClassAOutputContainer(result, status, error, metadata)
```

The goal is to keep call signatures conceptually to one input unit and one output unit.

## 4. Compresser granularity

Per-class example:

```text
ClassACompresser
  -> ClassAInputContainer
  -> ClassAOutputContainer
```

Feature-level example:

```text
UserFeatureCompresser
  -> CreateUserInputContainer
  -> CreateUserOutputContainer
  -> UpdateUserInputContainer
  -> UpdateUserOutputContainer
```

Both are valid. Compresser granularity and Container responsibility boundaries are separate concerns:

```text
Compresser = may support several related classes
Container  = remains separated per class
```

## 5. Sending multiple Containers

One communication does not need to contain only one Container:

```text
MessagePackage
  ClassAInputContainer
  ClassBInputContainer
  ClassCInputContainer
```

Distinguish these rules:

```text
1 class         = normally 1 Input Container + 1 Output Container
1 communication = may carry one or more Containers
```

Do not destroy responsibility boundaries by combining them into a giant `CommonContainer`.

## 6. Receiving Containers

The receiver may continue to work with the Container/Package rather than immediately exploding every value into many local arguments. Processing, which owns the semantic use of values, may inspect fields as needed. Messenger must not make decisions based on those field meanings.

## 7. Responsibilities

Compresser may:

- pack multiple inputs into an Input Container
- pack multiple returns into an Output Container
- provide extraction structure
- pack multiple Containers into a Package/Message
- follow communication-contract field layout
- create request/response Containers
- prepare non-business serialization representation

Compresser must not:

- calculate game/business results
- evaluate business rules
- decide UI presentation
- construct DB queries as business/data processing
- semantically transform save data
- choose processing order instead of Commander
- choose destinations instead of Messenger

## 8. Relationship with Commander / Messenger / Processing

```text
Commander
  decides what to call

Messenger
  transports communication

Compresser
  groups class-specific input/output into Containers
  optionally groups multiple Containers into a Package

Processing
  performs real work using the semantic meaning of Container values
```

Commander/Messenger may handle routing metadata such as `command`, `request_id`, and destination information, but should not inspect individual payload values for business decisions.

## 9. Request / Response separation

When request and response structures differ, use separate Input/Output Containers such as `CreateUserInputContainer` and `CreateUserOutputContainer`.

A Compresser may still be per class or per related feature, but should not grow into an unlimited catch-all responsibility.

## 10. Independence from transport

Containers/Packages should be independent of transport where practical, so they can survive changes between direct calls, event/message queues, IPC, sockets, HTTP, async/await, separate processes, or cross-language communication.

The relay-level model should remain:

```text
send Container / Package
receive Container / Package
```

## 11. Relationship with Message Contract

Compresser builds Containers/Packages according to `message-contract.en.md`. It must not independently extend the contract. Contract changes are made in the Message Contract first, then reflected in each Compresser.
