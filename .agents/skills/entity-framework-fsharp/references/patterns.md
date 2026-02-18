# Freetool EF + F# Mapping Patterns

## Table of Contents
- Value object to scalar mapping
- Option to nullable/null mapping
- Complex list to JSON TEXT mapping
- Discriminated union mapping patterns
- API JSON converters for domain types
- Event payload serialization/deserialization
- Repository usage pattern
- Placement map and anti-patterns
- Implementation checklist for new complex types

## Value Object to Scalar Mapping

Reference: `src/Freetool.Infrastructure/src/Database/FreetoolDbContext.fs`

```fsharp
let resourceIdConverter =
    ValueConverter<Freetool.Domain.ValueObjects.ResourceId, System.Guid>(
        (fun resourceId -> resourceId.Value),
        (fun guid -> Freetool.Domain.ValueObjects.ResourceId(guid))
    )

entity.Property(fun r -> r.Id).HasConversion(resourceIdConverter) |> ignore
```

Use this for ID wrappers and value objects with a single primitive backing value.

## Option to Nullable/Null Mapping

```fsharp
let optionStringConverter =
    ValueConverter<string option, string>(
        (fun opt -> match opt with | Some s -> s | None -> null),
        (fun str -> if isNull str then None else Some str)
    )

let optionDateTimeConverter =
    ValueConverter<DateTime option, System.Nullable<DateTime>>(
        (fun opt -> match opt with | Some dt -> System.Nullable(dt) | None -> System.Nullable()),
        (fun nullable -> if nullable.HasValue then Some(nullable.Value) else None)
    )
```

Use this for optional fields persisted in scalar columns.

## Complex List to JSON TEXT Mapping

Reference patterns: `RunData.InputValues`, `AppData.Inputs`, `ResourceData.UrlParameters`

```fsharp
let keyValuePairListConverter =
    ValueConverter<Freetool.Domain.ValueObjects.KeyValuePair list, string>(
        (fun kvps ->
            let serializable = kvps |> List.map (fun kvp -> {| Key = kvp.Key; Value = kvp.Value |})
            System.Text.Json.JsonSerializer.Serialize(serializable)),
        (fun json ->
            let deserialized =
                System.Text.Json.JsonSerializer.Deserialize<{| Key: string; Value: string |} list>(json)

            deserialized
            |> List.map (fun item ->
                match Freetool.Domain.ValueObjects.KeyValuePair.Create(item.Key, item.Value) with
                | Ok kvp -> kvp
                | Error _ -> failwith $"Invalid KeyValuePair in database: {item.Key}={item.Value}"))
    )

entity.Property(fun r -> r.Headers).HasConversion(keyValuePairListConverter) |> ignore
```

Use explicit serializable intermediary shapes; avoid serializing domain records directly when they may evolve.

## Discriminated Union Mapping Patterns

### Pattern A: DU <-> string via canonical `ToString` and parser

Reference: `InputType`, `SqlQueryMode`, `SqlFilterOperator`, `SqlSortDirection`

```fsharp
let serializeInputType (inputType: InputType) = inputType.ToString()

let deserializeInputType (str: string) =
    match str with
    | "Email" -> InputType.Email()
    | "Date" -> InputType.Date()
    | typeStr when typeStr.StartsWith("Text(") && typeStr.EndsWith(")") ->
        let lengthStr = typeStr.Substring(5, typeStr.Length - 6)
        match System.Int32.TryParse(lengthStr) with
        | (true, maxLength) ->
            match InputType.Text(maxLength) with
            | Ok valid -> valid
            | Error _ -> failwith $"Invalid Text type format in database: {typeStr}"
        | _ -> failwith $"Invalid Text type format in database: {typeStr}"
    | _ -> failwith $"Unknown InputType format in database: {str}"

let inputTypeConverter = ValueConverter<InputType, string>(serializeInputType, deserializeInputType)
```

### Pattern B: DU-containing record <-> JSON TEXT with explicit enum string conversions

Reference: `AppData.SqlConfig` converter

```fsharp
let sqlModeToString (mode: SqlQueryMode) =
    match mode with
    | SqlQueryMode.Gui -> "gui"
    | SqlQueryMode.Raw -> "raw"

let sqlModeFromString (value: string) =
    match value.Trim().ToLowerInvariant() with
    | "gui" -> SqlQueryMode.Gui
    | "raw" -> SqlQueryMode.Raw
    | _ -> failwith $"Invalid SqlQueryMode in database: {value}"

let sqlConfigConverter =
    ValueConverter<SqlQueryConfig option, string>(
        (fun configOpt ->
            match configOpt with
            | None -> null
            | Some config ->
                let serialized =
                    {| Mode = sqlModeToString config.Mode
                       Table = config.Table
                       Columns = config.Columns |}
                System.Text.Json.JsonSerializer.Serialize(serialized)),
        (fun json ->
            if System.String.IsNullOrWhiteSpace(json) then None
            else
                let deserialized = System.Text.Json.JsonSerializer.Deserialize<{| Mode: string; Table: string option; Columns: string list |}>(json)
                Some
                    { Mode = sqlModeFromString deserialized.Mode
                      Table = deserialized.Table
                      Columns = deserialized.Columns
                      Filters = []
                      Limit = None
                      OrderBy = []
                      RawSql = None
                      RawSqlParams = [] })
    )
```

## API JSON Converters for Domain Types

Reference: `src/Freetool.Application/src/DTOs/JsonConverters.fs` and `src/Freetool.Api/src/Program.fs`

```fsharp
type HttpMethodConverter() =
    inherit JsonConverter<HttpMethod>()

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) =
        let methodStr = reader.GetString()
        match HttpMethod.Create(methodStr) with
        | Ok httpMethod -> httpMethod
        | Error _ -> raise (JsonException($"Invalid HTTP method: {methodStr}"))

    override _.Write(writer: Utf8JsonWriter, value: HttpMethod, _options: JsonSerializerOptions) =
        writer.WriteStringValue(value.ToString())
```

Registration:

```fsharp
options.JsonSerializerOptions.Converters.Add(HttpMethodConverter())
options.JsonSerializerOptions.Converters.Add(JsonFSharpConverter(allowOverride = true))
```

## Event Payload Serialization/Deserialization

Reference: `EventRepository.fs`, `EventEnhancementService.fs`

```fsharp
let jsonOptions = JsonSerializerOptions()
jsonOptions.Converters.Add(JsonFSharpConverter())

// Important: do not omit null option fields when storing events.
let payload = JsonSerializer.Serialize(domainEvent, jsonOptions)
```

When reading:

```fsharp
let evt = JsonSerializer.Deserialize<Freetool.Domain.Events.AppUpdatedEvent>(eventData, jsonOptions)
```

## Repository Usage Pattern

Reference: `src/Freetool.Infrastructure/src/Database/Repositories/AppRepository.fs`

```fsharp
// Query using domain types directly; EF ValueConverter handles translation.
let! appData = context.Apps.FirstOrDefaultAsync(fun a -> a.Id = appId)

let! folderIds =
    context.Folders
        .Where(fun f -> spaceIdArray.Contains(f.SpaceId))
        .Select(fun f -> f.Id)
        .ToListAsync()
```

Avoid converting IDs to `Guid` in queries unless required by SQL expression translation.

## Placement Map and Anti-Patterns

Do:
- Put DB conversion logic in `FreetoolDbContext.fs`.
- Put HTTP JSON conversion logic in `Application/DTOs/JsonConverters.fs` and register in `Api/Program.fs`.
- Keep validation in domain `Create` functions.
- Keep repository code thin and type-safe.

Do not:
- Put `ValueConverter` logic in Domain.
- Parse ad-hoc JSON inside controllers for domain models.
- Duplicate conversion logic in multiple repositories.
- Store unvalidated strings when a domain constructor exists.

## Implementation Checklist for New Complex Types

1. Define/extend domain type with `Create` and `ToString` (or stable structured representation).
2. Add DB migration for new column(s) if needed.
3. Add EF `ValueConverter` and `entity.Property(...).HasConversion(...)` in `FreetoolDbContext.fs`.
4. Add API `JsonConverter` if type appears in requests/responses.
5. Register API converter in `Program.fs`.
6. Update repositories only if query/use-case logic changed.
7. Add tests:
- round-trip domain value -> DB -> domain value
- invalid DB payload behavior
- API JSON serialization and deserialization for the type
8. Run:
- `dotnet build Freetool.sln -c Release`
- `dotnet test Freetool.sln`
