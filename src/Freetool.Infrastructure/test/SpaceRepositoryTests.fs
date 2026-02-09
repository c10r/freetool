module Freetool.Infrastructure.Tests.SpaceRepositoryTests

open System
open System.Linq
open System.Threading.Tasks
open Microsoft.Data.Sqlite
open Microsoft.EntityFrameworkCore
open Xunit
open Freetool.Application.DTOs
open Freetool.Application.Interfaces
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Infrastructure.Database
open Freetool.Infrastructure.Database.Repositories

type NoOpEventRepository() =
    interface IEventRepository with
        member _.SaveEventAsync(_event: IDomainEvent) = Task.FromResult(())
        member _.CommitAsync() = Task.FromResult(())

        member _.GetEventsAsync(_filter: EventFilter) =
            Task.FromResult(
                { Items = []
                  TotalCount = 0
                  Skip = 0
                  Take = 0 }
            )

        member _.GetEventsByAppIdAsync(_filter: AppEventFilter) =
            Task.FromResult(
                { Items = []
                  TotalCount = 0
                  Skip = 0
                  Take = 0 }
            )

        member _.GetEventsByUserIdAsync(_filter: UserEventFilter) =
            Task.FromResult(
                { Items = []
                  TotalCount = 0
                  Skip = 0
                  Take = 0 }
            )

let private createSqliteContext () =
    let connection = new SqliteConnection("Data Source=:memory:")
    connection.Open()

    let options =
        DbContextOptionsBuilder<FreetoolDbContext>().UseSqlite(connection).Options

    let context = new FreetoolDbContext(options)
    context.Database.EnsureCreated() |> ignore
    context, connection

let private createUserData (userId: UserId) (email: string) (name: string) =
    { Id = userId
      Name = name
      Email = email
      ProfilePicUrl = None
      CreatedAt = DateTime.UtcNow
      UpdatedAt = DateTime.UtcNow
      IsDeleted = false
      InvitedAt = None }

[<Fact>]
let ``GetByNameAsync returns space when persisted SpaceData has null MemberIds`` () : Task =
    task {
        let context, connection = createSqliteContext ()
        use context = context
        use connection = connection
        let repository = SpaceRepository(context, NoOpEventRepository()) :> ISpaceRepository

        let moderatorUserId = UserId.NewId()
        let memberUserId = UserId.NewId()
        let spaceId = SpaceId.NewId()

        context.Users.Add(createUserData moderatorUserId "moderator@example.com" "Moderator")
        |> ignore

        context.Users.Add(createUserData memberUserId "member@example.com" "Member")
        |> ignore

        context.Spaces.Add(
            { Id = spaceId
              Name = "Engineering"
              ModeratorUserId = moderatorUserId
              CreatedAt = DateTime.UtcNow
              UpdatedAt = DateTime.UtcNow
              IsDeleted = false
              MemberIds = Unchecked.defaultof<UserId list> }
        )
        |> ignore

        context.SpaceMembers.Add(
            { Id = Guid.NewGuid()
              UserId = memberUserId
              SpaceId = spaceId
              CreatedAt = DateTime.UtcNow }
        )
        |> ignore

        let! _ = context.SaveChangesAsync()
        context.ChangeTracker.Clear()

        let! result = repository.GetByNameAsync("Engineering")

        Assert.True(result.IsSome)
        let space = result.Value
        Assert.False(isNull (box space.State.MemberIds))
        Assert.Single(space.State.MemberIds) |> ignore
        Assert.Equal(memberUserId, space.State.MemberIds.Head)
    }

[<Fact>]
let ``AddAsync succeeds when space has null MemberIds`` () : Task =
    task {
        let context, connection = createSqliteContext ()
        use context = context
        use connection = connection
        let repository = SpaceRepository(context, NoOpEventRepository()) :> ISpaceRepository

        let moderatorUserId = UserId.NewId()
        let spaceId = SpaceId.NewId()

        context.Users.Add(createUserData moderatorUserId "moderator2@example.com" "Moderator 2")
        |> ignore

        let malformedSpace =
            Space.fromData
                { Id = spaceId
                  Name = "QA"
                  ModeratorUserId = moderatorUserId
                  CreatedAt = DateTime.UtcNow
                  UpdatedAt = DateTime.UtcNow
                  IsDeleted = false
                  MemberIds = Unchecked.defaultof<UserId list> }

        let! addResult = repository.AddAsync(malformedSpace)

        Assert.True(addResult.IsOk)
        Assert.Equal(1, context.Spaces.Count())
        Assert.Equal("QA", context.Spaces.Single().Name)
    }
