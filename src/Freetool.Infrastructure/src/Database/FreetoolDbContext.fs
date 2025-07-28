namespace Freetool.Infrastructure.Database

open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Storage.ValueConversion

type FreetoolDbContext(options: DbContextOptions<FreetoolDbContext>) =
    inherit DbContext(options)

    [<DefaultValue>]
    val mutable private _users: DbSet<UserEntity>

    [<DefaultValue>]
    val mutable private _resources: DbSet<ResourceEntity>

    member this.Users
        with get () = this._users
        and set value = this._users <- value

    member this.Resources
        with get () = this._resources
        and set value = this._resources <- value

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