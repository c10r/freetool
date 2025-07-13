namespace Freetool.Infrastructure.Database

open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Storage.ValueConversion

type FreetoolDbContext(options: DbContextOptions<FreetoolDbContext>) =
    inherit DbContext(options)

    [<DefaultValue>]
    val mutable private _users: DbSet<UserEntity>

    member this.Users
        with get () = this._users
        and set value = this._users <- value

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