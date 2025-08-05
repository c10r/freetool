namespace Freetool.Infrastructure.Database

open System
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Storage.ValueConversion
open Freetool.Domain.Entities

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
            entity.Property(fun u -> u.ProfilePicUrl).HasConversion(optionStringConverter)
            |> ignore)
        |> ignore

        // Configure FolderData
        modelBuilder.Entity<FolderData>(fun entity ->
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

            entity.Property(fun f -> f.ParentId).HasConversion(optionFolderIdConverter)
            |> ignore)
        |> ignore

        // Configure RunData
        modelBuilder.Entity<RunData>(fun entity ->
            let optionDateTimeConverter =
                ValueConverter<DateTime option, System.Nullable<DateTime>>(
                    (fun opt ->
                        match opt with
                        | Some dt -> System.Nullable(dt)
                        | None -> System.Nullable()),
                    (fun nullable -> if nullable.HasValue then Some(nullable.Value) else None)
                )

            entity
                .Property(fun r -> r.StartedAt)
                .HasConversion<System.Nullable<DateTime>>(optionDateTimeConverter)
            |> ignore

            entity
                .Property(fun r -> r.CompletedAt)
                .HasConversion<System.Nullable<DateTime>>(optionDateTimeConverter)
            |> ignore)
        |> ignore

        // Configure foreign key relationships
        modelBuilder.Entity<UserGroupData>(fun entity ->
            entity.HasOne<UserData>().WithMany().HasForeignKey(fun ug -> ug.UserId :> obj)
            |> ignore

            entity.HasOne<GroupData>().WithMany().HasForeignKey(fun ug -> ug.GroupId :> obj)
            |> ignore)
        |> ignore

        modelBuilder.Entity<RunData>(fun entity ->
            entity
                .HasOne<AppData>()
                .WithMany()
                .HasForeignKey(fun r -> (r.AppId.Value) :> obj)
            |> ignore)
        |> ignore