# Data Commander Communication

[日本語](data-commander-communication.md) | **English**

Direct communication between Data-layer Commanders is discouraged in principle.

```text
Data Commander A
  X
Data Commander B
```

Each Data Commander controls its own Data-side responsibility. When Data Commanders begin to depend directly on one another, an additional internal control flow appears inside the Data layer and responsibility boundaries and call origins become harder to understand.

When coordination with another Data responsibility is required, normally return the result to an upper Commander so it can select the next operation. When crossing an Application boundary, use an explicit boundary such as Messenger or Contract.

Because this structure does not necessarily mean immediate architectural failure, the static checker reports a warning rather than an error.

## Checker rule

- `UPD103`: warning when a Data Commander directly depends on another Data Commander
- when the target is an internal Commander in another Application, the stronger `UPD102` rule takes priority
- justified exceptions may use the normal Ignore mechanism
