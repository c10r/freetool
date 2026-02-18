---
name: entity-framework-fsharp
description: Explain and implement how Freetool maps F# value objects, options, records, and discriminated unions across JSON and Entity Framework Core boundaries. Use when adding or changing persistence for complex F# types, creating EF ValueConverters in SQLite-backed repositories, wiring API JsonConverters, or documenting architecture placement for serialization in onion/hexagonal architecture.
---

# Entity Framework Fsharp

## Overview

Use this skill to add or review F# type mappings end-to-end: API JSON <-> DTO/domain types <-> EF Core <-> SQLite storage.

Prefer strict boundary mapping:
- Domain owns business types and validation.
- Application/API owns HTTP JSON converter behavior.
- Infrastructure owns DB converters and storage serialization.

## Architecture Placement

Keep mapping responsibilities in these layers.

1. Domain (`src/Freetool.Domain/src`)
- Define value objects and discriminated unions.
- Expose canonical parsing/printing APIs (`Create`, `ToString`, helpers).
- Keep persistence-agnostic; do not reference EF or `DbContext` here.

2. Application (`src/Freetool.Application/src`)
- Define `System.Text.Json` converters for API request/response shape.
- Convert primitive JSON tokens to validated domain values.
- Keep converters focused on transport; do not encode SQL/EF concerns.

3. API (`src/Freetool.Api/src/Program.fs`)
- Register all JSON converters globally.
- Keep serializer policy consistent (camelCase, null handling, F# converter).

4. Infrastructure (`src/Freetool.Infrastructure/src`)
- Implement EF `ValueConverter<DomainType, ProviderType>` in `FreetoolDbContext.fs`.
- Serialize complex DB columns (JSON TEXT columns) with stable DTO-like shapes.
- Repositories query by domain types directly and rely on ValueConverter translation.

5. Repositories (`src/Freetool.Infrastructure/src/Database/Repositories`)
- Persist `*.State` records and events.
- Avoid duplicating type conversion logic that belongs in `DbContext` or API converters.

## Workflow

1. Define/extend domain type
- Add/adjust `Create`, `ToString`, and invariant checks.
- Keep round-trip shape stable for serialization (`ToString` format or structured record).

2. Map API JSON boundary
- Add/update converter in `src/Freetool.Application/src/DTOs/JsonConverters.fs`.
- Register it in `src/Freetool.Api/src/Program.fs`.

3. Map EF/SQLite boundary
- Add `ValueConverter` in `src/Freetool.Infrastructure/src/Database/FreetoolDbContext.fs`.
- For complex nested structures, serialize to JSON TEXT with an explicit serializable shape.

4. Validate repository usage
- Query using domain value objects directly (for EF translation).
- Keep Save/Add/Update code free of ad-hoc string/Guid conversions.

5. Run checks
- `dotnet build Freetool.sln -c Release`
- `dotnet test Freetool.sln`

## Use These Patterns

Load `references/patterns.md` and adapt the templates:
- Value object <-> scalar converter
- `option` <-> nullable converter
- DU <-> string/JSON converter
- Nested record/list <-> JSON TEXT converter
- API `JsonConverter` + `Program.fs` registration
- Placement rules and anti-patterns

## Guardrails

- Prefer failing fast on invalid DB data in EF converters (`failwith` with context).
- Use domain `Create` functions when rebuilding value objects from storage.
- Keep event JSON options explicit when serializing F# options (`JsonFSharpConverter`).
- Do not suppress null fields in stored event payloads that need round-trip deserialization.
