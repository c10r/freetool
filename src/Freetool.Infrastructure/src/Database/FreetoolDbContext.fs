namespace Freetool.Infrastructure.Database

open System
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Storage.ValueConversion

type FreetoolDbContext(options: DbContextOptions<FreetoolDbContext>) =
    inherit DbContext(options)

    [<DefaultValue>]
    val mutable private _users: DbSet<UserEntity>

    [<DefaultValue>]
    val mutable private _resources: DbSet<ResourceEntity>

    [<DefaultValue>]
    val mutable private _folders: DbSet<FolderEntity>

    [<DefaultValue>]
    val mutable private _apps: DbSet<AppEntity>

    [<DefaultValue>]
    val mutable private _events: DbSet<EventEntity>

    member this.Users
        with get () = this._users
        and set value = this._users <- value

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

    override this.OnModelCreating(modelBuilder: ModelBuilder) =
        base.OnModelCreating modelBuilder

        modelBuilder.Entity<UserEntity>(fun entity ->
            entity.HasKey(fun u -> u.Id :> obj) |> ignore

            entity.HasIndex(fun u -> u.Email :> obj).IsUnique() |> ignore

            entity.Property(fun u -> u.Name :> obj).IsRequired().HasMaxLength(100) |> ignore

            entity.Property(fun u -> u.Email :> obj).IsRequired().HasMaxLength(254)
            |> ignore

            let optionStringConverter =
                ValueConverter<string option, string>(
                    (fun opt ->
                        match opt with
                        | Some s -> s
                        | None -> null),
                    (fun str -> if isNull str then None else Some str)
                )

            entity
                .Property(fun u -> u.ProfilePicUrl)
                .HasMaxLength(500)
                .HasConversion(optionStringConverter)
            |> ignore

            entity.Property(fun u -> u.CreatedAt :> obj).IsRequired() |> ignore

            entity.Property(fun u -> u.UpdatedAt :> obj).IsRequired() |> ignore)
        |> ignore

        modelBuilder.Entity<ResourceEntity>(fun entity ->
            entity.HasKey(fun r -> r.Id :> obj) |> ignore

            entity.HasIndex(fun r -> r.Name :> obj).IsUnique() |> ignore

            entity.Property(fun r -> r.Name :> obj).IsRequired().HasMaxLength(100) |> ignore

            entity.Property(fun r -> r.Description :> obj).IsRequired().HasMaxLength(500)
            |> ignore

            entity.Property(fun r -> r.BaseUrl :> obj).IsRequired().HasMaxLength(1000)
            |> ignore

            entity.Property(fun r -> r.UrlParameters :> obj).IsRequired() |> ignore

            entity.Property(fun r -> r.Headers :> obj).IsRequired() |> ignore

            entity.Property(fun r -> r.Body :> obj).IsRequired() |> ignore

            entity.Property(fun r -> r.CreatedAt :> obj).IsRequired() |> ignore

            entity.Property(fun r -> r.UpdatedAt :> obj).IsRequired() |> ignore)
        |> ignore

        modelBuilder.Entity<FolderEntity>(fun entity ->
            entity.HasKey(fun f -> f.Id :> obj) |> ignore

            entity.HasIndex([| "Name"; "ParentId" |]).IsUnique() |> ignore

            entity.Property(fun f -> f.Name :> obj).IsRequired().HasMaxLength(100) |> ignore

            entity.Property(fun f -> f.ParentId :> obj) |> ignore

            entity.Property(fun f -> f.CreatedAt :> obj).IsRequired() |> ignore

            entity.Property(fun f -> f.UpdatedAt :> obj).IsRequired() |> ignore)
        |> ignore

        modelBuilder.Entity<AppEntity>(fun entity ->
            entity.HasKey(fun a -> a.Id :> obj) |> ignore

            entity.HasIndex([| "Name"; "FolderId" |]).IsUnique() |> ignore

            entity.Property(fun a -> a.Name :> obj).IsRequired().HasMaxLength(100) |> ignore

            entity.Property(fun a -> a.FolderId :> obj).IsRequired() |> ignore

            entity.Property(fun a -> a.Inputs :> obj).IsRequired() |> ignore

            entity.Property(fun a -> a.CreatedAt :> obj).IsRequired() |> ignore

            entity.Property(fun a -> a.UpdatedAt :> obj).IsRequired() |> ignore)
        |> ignore

        modelBuilder.Entity<EventEntity>(fun entity ->
            entity.HasKey(fun e -> e.Id :> obj) |> ignore

            entity.HasIndex(fun e -> e.EventId :> obj).IsUnique() |> ignore

            entity.HasIndex([| "EntityType"; "EntityId" |]) |> ignore

            entity.Property(fun e -> e.EventId :> obj).IsRequired().HasMaxLength(36)
            |> ignore

            entity.Property(fun e -> e.EventType :> obj).IsRequired().HasMaxLength(100)
            |> ignore

            entity.Property(fun e -> e.EntityType :> obj).IsRequired().HasMaxLength(100)
            |> ignore

            entity.Property(fun e -> e.EntityId :> obj).IsRequired().HasMaxLength(36)
            |> ignore

            entity.Property(fun e -> e.EventData :> obj).IsRequired() |> ignore

            entity.Property(fun e -> e.OccurredAt :> obj).IsRequired() |> ignore

            entity.Property(fun e -> e.CreatedAt :> obj).IsRequired() |> ignore

            // Prevent all delete operations on Events table - audit log should be immutable
            entity.HasQueryFilter(fun _ -> false) |> ignore)
        |> ignore

        // Add soft delete filters for all other entities
        modelBuilder.Entity<UserEntity>().HasQueryFilter(fun u -> not u.IsDeleted)
        |> ignore

        modelBuilder.Entity<ResourceEntity>().HasQueryFilter(fun r -> not r.IsDeleted)
        |> ignore

        modelBuilder.Entity<FolderEntity>().HasQueryFilter(fun f -> not f.IsDeleted)
        |> ignore

        modelBuilder.Entity<AppEntity>().HasQueryFilter(fun a -> not a.IsDeleted)
        |> ignore