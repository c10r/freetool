namespace Freetool.Infrastructure.Database

open System
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Storage.ValueConversion
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects

type FreetoolDbContext(options: DbContextOptions<FreetoolDbContext>) =
    inherit DbContext(options)

    [<DefaultValue>]
    val mutable private _users: DbSet<UserData>

    [<DefaultValue>]
    val mutable private _groups: DbSet<GroupData>

    [<DefaultValue>]
    val mutable private _userGroups: DbSet<UserGroupData>

    [<DefaultValue>]
    val mutable private _resources: DbSet<ResourceData>

    [<DefaultValue>]
    val mutable private _folders: DbSet<FolderData>

    [<DefaultValue>]
    val mutable private _apps: DbSet<AppData>

    [<DefaultValue>]
    val mutable private _events: DbSet<EventData>

    [<DefaultValue>]
    val mutable private _runs: DbSet<RunData>

    member this.Users
        with get () = this._users
        and set value = this._users <- value

    member this.Groups
        with get () = this._groups
        and set value = this._groups <- value

    member this.UserGroups
        with get () = this._userGroups
        and set value = this._userGroups <- value

    member this.Resources
        with get () = this._resources
        and set value = this._resources <- value

    member this.Folders
        with get () = this._folders
        and set value = this._folders <- value

    member this.Apps
        with get () = this._apps
        and set value = this._apps <- value

    member this.Events
        with get () = this._events
        and set value = this._events <- value

    member this.Runs
        with get () = this._runs
        and set value = this._runs <- value

    override this.OnModelCreating(modelBuilder: ModelBuilder) =
        base.OnModelCreating modelBuilder

        // Ignore value types that shouldn't be treated as entities
        modelBuilder.Ignore<Freetool.Domain.RunInputValue>() |> ignore
        modelBuilder.Ignore<Freetool.Domain.Events.Input>() |> ignore
        modelBuilder.Ignore<Freetool.Domain.ExecutableHttpRequest>() |> ignore
        modelBuilder.Ignore<Freetool.Domain.ExecutableHttpRequest option>() |> ignore
        modelBuilder.Ignore<string option>() |> ignore

        // Set up value converters for custom types and complex objects
        let optionStringConverter =
            ValueConverter<string option, string>(
                (fun opt ->
                    match opt with
                    | Some s -> s
                    | None -> null),
                (fun str -> if isNull str then None else Some str)
            )

        // Configure UserData
        modelBuilder.Entity<UserData>(fun entity ->
            let userIdConverter =
                ValueConverter<Freetool.Domain.ValueObjects.UserId, System.Guid>(
                    (fun userId -> userId.Value),
                    (fun guid -> Freetool.Domain.ValueObjects.UserId(guid))
                )

            entity.Property(fun u -> u.Id).HasConversion(userIdConverter) |> ignore

            entity.Property(fun u -> u.ProfilePicUrl).HasConversion(optionStringConverter)
            |> ignore)
        |> ignore

        // Configure FolderData
        modelBuilder.Entity<FolderData>(fun entity ->
            let folderIdConverter =
                ValueConverter<Freetool.Domain.ValueObjects.FolderId, System.Guid>(
                    (fun folderId -> folderId.Value),
                    (fun guid -> Freetool.Domain.ValueObjects.FolderId(guid))
                )

            let folderNameConverter =
                ValueConverter<Freetool.Domain.ValueObjects.FolderName, string>(
                    (fun folderName -> folderName.Value),
                    (fun str ->
                        match Freetool.Domain.ValueObjects.FolderName.Create(Some str) with
                        | Ok validName -> validName
                        | Error _ -> failwith $"Invalid FolderName in database: {str}")
                )

            let optionFolderIdConverter =
                ValueConverter<Freetool.Domain.ValueObjects.FolderId option, System.Nullable<System.Guid>>(
                    (fun opt ->
                        match opt with
                        | Some folderId -> System.Nullable(folderId.Value)
                        | None -> System.Nullable()),
                    (fun nullable ->
                        if nullable.HasValue then
                            Some(Freetool.Domain.ValueObjects.FolderId(nullable.Value))
                        else
                            None)
                )

            // Explicit property configuration to help with constructor binding
            entity
                .Property(fun f -> f.Id)
                .HasColumnName("Id")
                .HasConversion(folderIdConverter)
            |> ignore

            entity
                .Property(fun f -> f.Name)
                .HasColumnName("Name")
                .HasConversion(folderNameConverter)
            |> ignore

            entity.Property(fun f -> f.CreatedAt).HasColumnName("CreatedAt") |> ignore
            entity.Property(fun f -> f.UpdatedAt).HasColumnName("UpdatedAt") |> ignore
            entity.Property(fun f -> f.IsDeleted).HasColumnName("IsDeleted") |> ignore

            // Ignore the Children navigation property explicitly
            entity.Ignore("Children") |> ignore

            entity.Property(fun f -> f.ParentId).HasConversion(optionFolderIdConverter)
            |> ignore)
        |> ignore

        // Configure RunData
        modelBuilder.Entity<RunData>(fun entity ->
            let runIdConverter =
                ValueConverter<Freetool.Domain.ValueObjects.RunId, System.Guid>(
                    (fun runId -> runId.Value),
                    (fun guid -> Freetool.Domain.ValueObjects.RunId(guid))
                )

            let appIdConverter =
                ValueConverter<Freetool.Domain.ValueObjects.AppId, System.Guid>(
                    (fun appId -> appId.Value),
                    (fun guid -> Freetool.Domain.ValueObjects.AppId(guid))
                )

            let runStatusConverter =
                ValueConverter<Freetool.Domain.ValueObjects.RunStatus, string>(
                    (fun runStatus -> runStatus.ToString()),
                    (fun str ->
                        match Freetool.Domain.ValueObjects.RunStatus.Create(str) with
                        | Ok validStatus -> validStatus
                        | Error _ -> failwith $"Invalid RunStatus in database: {str}")
                )

            let optionDateTimeConverter =
                ValueConverter<DateTime option, System.Nullable<DateTime>>(
                    (fun opt ->
                        match opt with
                        | Some dt -> System.Nullable(dt)
                        | None -> System.Nullable()),
                    (fun nullable -> if nullable.HasValue then Some(nullable.Value) else None)
                )

            entity.Property(fun r -> r.Id).HasConversion(runIdConverter) |> ignore
            entity.Property(fun r -> r.AppId).HasConversion(appIdConverter) |> ignore
            entity.Property(fun r -> r.Status).HasConversion(runStatusConverter) |> ignore

            entity
                .Property(fun r -> r.StartedAt)
                .HasConversion<System.Nullable<DateTime>>(optionDateTimeConverter)
            |> ignore

            entity
                .Property(fun r -> r.CompletedAt)
                .HasConversion<System.Nullable<DateTime>>(optionDateTimeConverter)
            |> ignore

            entity.HasOne<AppData>().WithMany().HasForeignKey(fun r -> r.AppId :> obj)
            |> ignore)
        |> ignore

        // Configure ResourceData
        modelBuilder.Entity<ResourceData>(fun entity ->

            let resourceIdConverter =
                ValueConverter<Freetool.Domain.ValueObjects.ResourceId, System.Guid>(
                    (fun resourceId -> resourceId.Value),
                    (fun guid -> Freetool.Domain.ValueObjects.ResourceId(guid))
                )

            let resourceNameConverter =
                ValueConverter<Freetool.Domain.ValueObjects.ResourceName, string>(
                    (fun resourceName -> resourceName.Value),
                    (fun str ->
                        match Freetool.Domain.ValueObjects.ResourceName.Create(Some str) with
                        | Ok validName -> validName
                        | Error _ -> failwith $"Invalid ResourceName in database: {str}")
                )

            let resourceDescriptionConverter =
                ValueConverter<Freetool.Domain.ValueObjects.ResourceDescription, string>(
                    (fun resourceDescription -> resourceDescription.Value),
                    (fun str ->
                        match Freetool.Domain.ValueObjects.ResourceDescription.Create(Some str) with
                        | Ok validDescription -> validDescription
                        | Error _ -> failwith $"Invalid ResourceDescription in database: {str}")
                )

            let keyValuePairListConverter =
                ValueConverter<Freetool.Domain.ValueObjects.KeyValuePair list, string>(
                    (fun kvps ->
                        let serializable =
                            kvps |> List.map (fun kvp -> {| Key = kvp.Key; Value = kvp.Value |})

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

            let baseUrlConverter =
                ValueConverter<Freetool.Domain.ValueObjects.BaseUrl, string>(
                    (fun baseUrl -> baseUrl.Value),
                    (fun str ->
                        match Freetool.Domain.ValueObjects.BaseUrl.Create(Some str) with
                        | Ok validBaseUrl -> validBaseUrl
                        | Error _ -> failwith $"Invalid BaseUrl in database: {str}")
                )

            let httpMethodConverter =
                ValueConverter<Freetool.Domain.ValueObjects.HttpMethod, string>(
                    (fun httpMethod -> httpMethod.ToString()),
                    (fun str ->
                        match Freetool.Domain.ValueObjects.HttpMethod.Create(str) with
                        | Ok validMethod -> validMethod
                        | Error _ -> failwith $"Invalid HttpMethod in database: {str}")
                )

            entity.Property(fun r -> r.Id).HasConversion(resourceIdConverter) |> ignore
            entity.Property(fun r -> r.Name).HasConversion(resourceNameConverter) |> ignore

            entity
                .Property(fun r -> r.Description)
                .HasConversion(resourceDescriptionConverter)
            |> ignore

            entity.Property(fun r -> r.HttpMethod).HasConversion(httpMethodConverter)
            |> ignore

            entity.Property(fun r -> r.BaseUrl).HasConversion(baseUrlConverter) |> ignore

            entity
                .Property(fun r -> r.UrlParameters)
                .HasConversion(keyValuePairListConverter)
            |> ignore

            entity.Property(fun r -> r.Headers).HasConversion(keyValuePairListConverter)
            |> ignore

            entity.Property(fun r -> r.Body).HasConversion(keyValuePairListConverter)
            |> ignore)
        |> ignore

        // Configure AppData
        modelBuilder.Entity<AppData>(fun entity ->
            let appIdConverter =
                ValueConverter<Freetool.Domain.ValueObjects.AppId, System.Guid>(
                    (fun appId -> appId.Value),
                    (fun guid -> Freetool.Domain.ValueObjects.AppId(guid))
                )

            let folderIdConverter =
                ValueConverter<Freetool.Domain.ValueObjects.FolderId, System.Guid>(
                    (fun folderId -> folderId.Value),
                    (fun guid -> Freetool.Domain.ValueObjects.FolderId(guid))
                )

            let resourceIdConverter =
                ValueConverter<Freetool.Domain.ValueObjects.ResourceId, System.Guid>(
                    (fun resourceId -> resourceId.Value),
                    (fun guid -> Freetool.Domain.ValueObjects.ResourceId(guid))
                )

            // Explicit property configuration to help with constructor binding
            entity.Property(fun a -> a.Id).HasColumnName("Id").HasConversion(appIdConverter)
            |> ignore

            entity.Property(fun a -> a.FolderId).HasConversion(folderIdConverter) |> ignore

            entity
                .Property(fun a -> a.ResourceId)
                .HasColumnName("ResourceId")
                .HasConversion(resourceIdConverter)
            |> ignore

            entity.Property(fun a -> a.CreatedAt).HasColumnName("CreatedAt") |> ignore
            entity.Property(fun a -> a.UpdatedAt).HasColumnName("UpdatedAt") |> ignore
            entity.Property(fun a -> a.IsDeleted).HasColumnName("IsDeleted") |> ignore

            // JSON converters for complex list types
            let serializeInputs (inputs: Freetool.Domain.Events.Input list) =
                let serializable =
                    inputs
                    |> List.map (fun input -> {|
                        Title = input.Title
                        Type = input.Type.ToString()
                        Required = input.Required
                    |})

                System.Text.Json.JsonSerializer.Serialize(serializable)

            let deserializeInputs (json: string) =
                let deserialized =
                    System.Text.Json.JsonSerializer.Deserialize<
                        {|
                            Title: string
                            Type: string
                            Required: bool
                        |} list
                     >(
                        json
                    )

                deserialized
                |> List.map (fun item ->
                    let inputType =
                        match item.Type with
                        | "Email" -> Ok(InputType.Email())
                        | "Date" -> Ok(InputType.Date())
                        | "Integer" -> Ok(InputType.Integer())
                        | "Boolean" -> Ok(InputType.Boolean())
                        | typeStr when typeStr.StartsWith("Text(") && typeStr.EndsWith(")") ->
                            let lengthStr = typeStr.Substring(5, typeStr.Length - 6)

                            match System.Int32.TryParse(lengthStr) with
                            | (true, maxLength) -> InputType.Text(maxLength)
                            | _ ->
                                Error(
                                    Freetool.Domain.ValidationError $"Invalid Text type format in database: {typeStr}"
                                )
                        | _ ->
                            Error(
                                Freetool.Domain.ValidationError $"Unknown InputType format in database: {item.Type}"
                            )

                    match inputType with
                    | Ok validInputType -> {
                        Freetool.Domain.Events.Input.Title = item.Title
                        Freetool.Domain.Events.Input.Type = validInputType
                        Freetool.Domain.Events.Input.Required = item.Required
                      }
                    | Error _ -> failwith $"Invalid InputType in database: {item.Type}")

            let inputListConverter =
                ValueConverter<Freetool.Domain.Events.Input list, string>(serializeInputs, deserializeInputs)

            // InputType converter for complex discriminated union stored as JSON
            let serializeInputType (inputType: InputType) = inputType.ToString()

            let deserializeInputType (str: string) =
                match str with
                | "Email" -> InputType.Email()
                | "Date" -> InputType.Date()
                | "Integer" -> InputType.Integer()
                | "Boolean" -> InputType.Boolean()
                | typeStr when typeStr.StartsWith("Text(") && typeStr.EndsWith(")") ->
                    let lengthStr = typeStr.Substring(5, typeStr.Length - 6)

                    match System.Int32.TryParse(lengthStr) with
                    | (true, maxLength) ->
                        match InputType.Text(maxLength) with
                        | Ok validInputType -> validInputType
                        | Error _ -> failwith $"Invalid Text type format in database: {typeStr}"
                    | _ -> failwith $"Invalid Text type format in database: {typeStr}"
                | _ -> failwith $"Unknown InputType format in database: {str}"

            let inputTypeConverter =
                ValueConverter<InputType, string>(serializeInputType, deserializeInputType)

            let keyValuePairListConverter =
                ValueConverter<Freetool.Domain.ValueObjects.KeyValuePair list, string>(
                    (fun kvps ->
                        let serializable =
                            kvps |> List.map (fun kvp -> {| Key = kvp.Key; Value = kvp.Value |})

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

            entity.Property(fun a -> a.Inputs).HasConversion<string>(inputListConverter)
            |> ignore

            entity.Property(fun a -> a.UrlPath).HasConversion(optionStringConverter)
            |> ignore

            entity
                .Property(fun a -> a.UrlParameters)
                .HasConversion(keyValuePairListConverter)
            |> ignore

            entity.Property(fun a -> a.Headers).HasConversion(keyValuePairListConverter)
            |> ignore

            entity.Property(fun a -> a.Body).HasConversion(keyValuePairListConverter)
            |> ignore)
        |> ignore

        // Configure EventData
        modelBuilder.Entity<EventData>(fun entity ->
            let eventTypeConverter =
                ValueConverter<EventType, string>(
                    (fun eventType -> EventTypeConverter.toString eventType),
                    (fun str ->
                        match EventTypeConverter.fromString str with
                        | Some eventType -> eventType
                        | None -> failwith $"Invalid EventType in database: {str}")
                )

            let entityTypeConverter =
                ValueConverter<EntityType, string>(
                    (fun entityType -> EntityTypeConverter.toString entityType),
                    (fun str ->
                        match EntityTypeConverter.fromString str with
                        | Some entityType -> entityType
                        | None -> failwith $"Invalid EntityType in database: {str}")
                )

            let userIdConverter =
                ValueConverter<Freetool.Domain.ValueObjects.UserId, System.Guid>(
                    (fun userId -> userId.Value),
                    (fun guid -> Freetool.Domain.ValueObjects.UserId(guid))
                )

            // Explicit property configuration to help with constructor binding
            entity.Property(fun e -> e.Id).HasColumnName("Id") |> ignore
            entity.Property(fun e -> e.EventId).HasColumnName("EventId") |> ignore

            entity
                .Property(fun e -> e.EventType)
                .HasColumnName("EventType")
                .HasConversion(eventTypeConverter)
            |> ignore

            entity
                .Property(fun e -> e.EntityType)
                .HasColumnName("EntityType")
                .HasConversion(entityTypeConverter)
            |> ignore

            entity.Property(fun e -> e.EntityId).HasColumnName("EntityId") |> ignore
            entity.Property(fun e -> e.EventData).HasColumnName("EventData") |> ignore
            entity.Property(fun e -> e.OccurredAt).HasColumnName("OccurredAt") |> ignore
            entity.Property(fun e -> e.CreatedAt).HasColumnName("CreatedAt") |> ignore
            entity.Property(fun e -> e.UserId).HasConversion(userIdConverter) |> ignore)
        |> ignore

        // Configure GroupData
        modelBuilder.Entity<GroupData>(fun entity ->
            let groupIdConverter =
                ValueConverter<Freetool.Domain.ValueObjects.GroupId, System.Guid>(
                    (fun groupId -> groupId.Value),
                    (fun guid -> Freetool.Domain.ValueObjects.GroupId(guid))
                )

            // Explicit property configuration to help with constructor binding
            entity
                .Property(fun g -> g.Id)
                .HasColumnName("Id")
                .HasConversion(groupIdConverter)
            |> ignore

            entity.Property(fun g -> g.Name).HasColumnName("Name") |> ignore
            entity.Property(fun g -> g.CreatedAt).HasColumnName("CreatedAt") |> ignore
            entity.Property(fun g -> g.UpdatedAt).HasColumnName("UpdatedAt") |> ignore
            entity.Property(fun g -> g.IsDeleted).HasColumnName("IsDeleted") |> ignore

            // Ignore the UserIds navigation properties explicitly
            entity.Ignore("UserIds") |> ignore
            entity.Ignore("_userIds") |> ignore)
        |> ignore

        // Configure UserGroupData
        modelBuilder.Entity<UserGroupData>(fun entity ->
            let userIdConverter =
                ValueConverter<Freetool.Domain.ValueObjects.UserId, System.Guid>(
                    (fun userId -> userId.Value),
                    (fun guid -> Freetool.Domain.ValueObjects.UserId(guid))
                )

            let groupIdConverter =
                ValueConverter<Freetool.Domain.ValueObjects.GroupId, System.Guid>(
                    (fun groupId -> groupId.Value),
                    (fun guid -> Freetool.Domain.ValueObjects.GroupId(guid))
                )

            entity.Property(fun ug -> ug.UserId).HasConversion(userIdConverter) |> ignore
            entity.Property(fun ug -> ug.GroupId).HasConversion(groupIdConverter) |> ignore

            // Configure foreign key relationships - now the types will match
            entity.HasOne<UserData>().WithMany().HasForeignKey(fun ug -> ug.UserId :> obj)
            |> ignore

            entity.HasOne<GroupData>().WithMany().HasForeignKey(fun ug -> ug.GroupId :> obj)
            |> ignore)
        |> ignore