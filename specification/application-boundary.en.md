# Application Boundary

[日本語](application-boundary.md) | **English**

In UPD Commander, an Application is not merely an executable or an entire product. It is an independently closed functional boundary containing one set of UI / Process / Data responsibilities.

## 1. Definition of Application

A feature should be treated as an independent Application when it satisfies many of the following conditions:

- it has its own screens
- it has its own processing flow
- it has its own state
- it has its own persistence/retrieval responsibilities
- it works without directly using Processing owned by another screen or feature
- communication with other features can be defined through an explicit boundary

A separate screen alone is not sufficient reason to split an Application. If only presentation differs while Process/Data responsibilities are shared, multiple UIs may remain inside the same Application.

## 2. Nested Application

When a product contains multiple independent features, split them into Sub Applications and apply UI / Process / Data again inside each one.

```text
Product
├─ MainApplication
│  ├─ UI
│  ├─ Process
│  └─ Data
│
└─ SettingsApplication
   ├─ UI
   ├─ Process
   └─ Data
```

The three-layer structure may therefore be applied recursively at every independent Application boundary, not only once at the product root.

## 3. Dependencies between Applications

An Application must not directly depend on another Application's internal implementation.

Prohibited examples:

```text
MainApplication/UI Processing
  -> SettingsApplication/Process Processing
```

```text
MainApplication/Process Processing
  -> SettingsApplication/Data Processing
```

When Applications must communicate, use one of the following explicit boundaries:

- an upper Application/Product Commander
- an explicit Messenger
- an inter-Application Contract / DTO / Message
- a Shared Contract that belongs to no specific Application

Do not place Processing or persistence logic into a shared area merely to bypass Application boundaries.

## 4. Rules inside each Application

Each Application independently follows:

```text
Application
├─ UI
├─ Process
└─ Data
```

The normal Commander / Messenger / Processing rules then apply within those layers.

Multiple Applications therefore should not normally share one giant global UI / Process / Data structure.

## 5. Examples

Treat as separate Applications when, for example:

- main and settings screens have independent processing, state, and persistence
- editor and preview each have independent processing systems
- launcher and game communicate only through an explicit message boundary

Keep as one Application when, for example:

- list and detail screens use the same Process/Data responsibilities
- desktop and mobile screens differ only in presentation
- tabs share the same processing responsibilities

## 6. Recommended directory structure

```text
applications/
├─ main/
│  ├─ ui/
│  ├─ process/
│  └─ data/
└─ settings/
   ├─ ui/
   ├─ process/
   └─ data/
```

Names such as `apps/`, `applications/`, or `features/` may be chosen by the project. What matters is that the Application boundary can be identified mechanically from paths or configuration.

## 7. Tooling

When a static checker can identify Application boundaries, it should evaluate UI / Process / Data independently for each Application and detect direct dependencies on internal layers or Processing of another Application.

When boundaries cannot be inferred, explicit configuration may be used.
