# Data Flow

[日本語](data-flow.md) | **English**

This document explains how requests and results move between UI, Process, and Data. The diagrams show responsibility flow; they do not prescribe a particular sequence of function calls or a synchronization model.

## Diagram notation

| Notation | Meaning |
| --- | --- |
| `--[call]-->` | A Commander asks a Processing component or Messenger in the same layer to do work |
| `--[result: ...]-->` | A called Processing component returns a result to its Commander. An additional-data request is also returned as a result |
| `==[message: ...]==>` | A Messenger carries a request or result to the Messenger in an adjacent layer |
| `--[handoff]-->` | A receiving Messenger passes the received content to a Commander in the same layer |

`result` describes how the processing outcome is handed over; it does not require the caller to wait for a synchronous response. Messenger communication can be synchronous (wait for the response) or asynchronous (receive the response later). For correlating multiple asynchronous requests, see the [Message Contract](../specification/message-contract.en.md).

`message` marks the boundary of communication responsibility; it does not require a message queue or asynchronous processing. A Messenger may implement the communication using a direct method call.

## Request flow

This route shows a request that starts in UI and needs additional data in Process.

```text
UI Commander --[call]--> UI Processing
UI Processing --[result: input value]--> UI Commander
UI Commander --[call]--> UI Messenger
UI Messenger ==[message: request]==> Process Messenger
Process Messenger --[handoff]--> Process Commander
Process Commander --[call]--> Process Processing
Process Processing --[result: additional-data request]--> Process Commander
Process Commander --[call]--> Process Messenger
Process Messenger ==[message: data request]==> Data Messenger
Data Messenger --[handoff]--> Data Commander
Data Commander --[call]--> Data Processing
```

When Process Processing needs additional data, it returns what it needs to its Commander as a processing result. Processing does not call a Messenger or send a request directly to another layer. The Commander receives that result and asks a Messenger to send the request.

## Response flow

This route returns retrieved data from Data, resumes Process work, and sends the final result to UI.

```text
Data Processing --[result: retrieved data]--> Data Commander
Data Commander --[call]--> Data Messenger
Data Messenger ==[message: retrieved data]==> Process Messenger
Process Messenger --[handoff]--> Process Commander
Process Commander --[call: resume processing]--> Process Processing
Process Processing --[result: final result]--> Process Commander
Process Commander --[call]--> Process Messenger
Process Messenger ==[message: final result]==> UI Messenger
UI Messenger --[handoff]--> UI Commander
UI Commander --[call]--> UI Processing
```

UI Processing may convert the final result for presentation. To persist data, Process Commander instead asks a Messenger to send a save request, which is handled in Data. UI Messenger and Data Messenger do not communicate directly in either route.

## One end-to-end example: product search

A user enters a keyword on a search screen, finds products, and displays the results.

1. UI Processing reads the keyword from the input field and returns it to UI Commander.
2. UI Commander passes the search request to UI Messenger, which sends a request message to Process Messenger.
3. Process Messenger hands the request to Process Commander, which calls Process Processing to handle the search conditions.
4. If Process Processing needs product information, it returns a “product search data needed” result to Process Commander. This result represents an additional request; Processing does not call Messenger directly.
5. Process Commander asks Process Messenger to send a data request. Process Messenger sends it to Data Messenger, and Data Commander calls the Data Processing operation that reads products.
6. Data Processing returns the retrieved data to Data Commander. Data Messenger sends a response message to Process Messenger, which hands the result to Process Commander. Process Commander resumes Process Processing.
7. When Process Processing returns the search result, Process Commander passes it to Process Messenger. Process Messenger sends a result message to UI Messenger, which hands it to UI Commander. UI Commander calls UI Processing to display the list.

Names such as `search request` and `additional request` are conceptual labels for explaining responsibilities. Choose concrete types, message names, and synchronous or asynchronous behavior to suit the implementation. For the overall layers and roles, see [Architecture](architecture.en.md); for the boundaries of Commander and Messenger, see [Commander & Messenger](commander-messenger.en.md).
