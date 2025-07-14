namespace Freetool.Domain.Tests.Events

open System
open Xunit
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events

module UserEventsTests =

    let createValidEmail () =
        Email.Create("test@example.com")
        |> Result.defaultWith (fun _ -> failwith "Invalid email in test setup")

    let createValidUserId () = UserId.NewId()

    let createValidUrl () =
        Url.Create("https://google.com")
        |> Result.defaultWith (fun _ -> failwith "Invalid url in test setup")

    [<Fact>]
    let ``userCreated creates UserCreatedEvent with correct properties`` () =
        let userId = createValidUserId ()
        let email = createValidEmail ()
        let name = "John Doe"
        let profilePicUrl = Some(createValidUrl ())

        let event = UserEvents.userCreated userId name email profilePicUrl

        Assert.Equal(userId, event.UserId)
        Assert.Equal(name, event.Name)
        Assert.Equal(email, event.Email)
        Assert.Equal(profilePicUrl, event.ProfilePicUrl)
        Assert.True(event.OccurredAt <= DateTime.UtcNow)
        Assert.NotEqual(Guid.Empty, event.EventId)

    [<Fact>]
    let ``userCreated creates UserCreatedEvent that implements IDomainEvent`` () =
        let userId = createValidUserId ()
        let email = createValidEmail ()
        let event = UserEvents.userCreated userId "John Doe" email None

        let domainEvent = event :> IDomainEvent
        Assert.Equal(event.OccurredAt, domainEvent.OccurredAt)
        Assert.Equal(event.EventId, domainEvent.EventId)

    [<Fact>]
    let ``userUpdated creates UserUpdatedEvent with correct properties`` () =
        let userId = createValidUserId ()
        let oldEmail = createValidEmail ()
        let newEmailResult = Email.Create("newemail@example.com")

        match newEmailResult with
        | Ok newEmail ->
            let changes = [ NameChanged("Old Name", "New Name"); EmailChanged(oldEmail, newEmail) ]

            let event = UserEvents.userUpdated userId changes

            Assert.Equal(userId, event.UserId)
            Assert.Equal<UserChange list>(changes, event.Changes)
            Assert.True(event.OccurredAt <= DateTime.UtcNow)
            Assert.NotEqual(Guid.Empty, event.EventId)
        | Error _ -> Assert.True(false, "Failed to create new email")

    [<Fact>]
    let ``userUpdated creates UserUpdatedEvent that implements IDomainEvent`` () =
        let userId = createValidUserId ()
        let changes = [ NameChanged("Old", "New") ]
        let event = UserEvents.userUpdated userId changes

        let domainEvent = event :> IDomainEvent
        Assert.Equal(event.OccurredAt, domainEvent.OccurredAt)
        Assert.Equal(event.EventId, domainEvent.EventId)

    [<Fact>]
    let ``userDeleted creates UserDeletedEvent with correct properties`` () =
        let userId = createValidUserId ()

        let event = UserEvents.userDeleted userId

        Assert.Equal(userId, event.UserId)
        Assert.True(event.OccurredAt <= DateTime.UtcNow)
        Assert.NotEqual(Guid.Empty, event.EventId)

    [<Fact>]
    let ``userDeleted creates UserDeletedEvent that implements IDomainEvent`` () =
        let userId = createValidUserId ()
        let event = UserEvents.userDeleted userId

        let domainEvent = event :> IDomainEvent
        Assert.Equal(event.OccurredAt, domainEvent.OccurredAt)
        Assert.Equal(event.EventId, domainEvent.EventId)

    [<Fact>]
    let ``NameChanged stores old and new values correctly`` () =
        let change = NameChanged("Old Name", "New Name")

        match change with
        | NameChanged(oldValue, newValue) ->
            Assert.Equal("Old Name", oldValue)
            Assert.Equal("New Name", newValue)
        | _ -> Assert.True(false, "Expected NameChanged")

    [<Fact>]
    let ``EmailChanged stores old and new emails correctly`` () =
        let oldEmail = createValidEmail ()
        let newEmailResult = Email.Create("newemail@example.com")

        match newEmailResult with
        | Ok newEmail ->
            let change = EmailChanged(oldEmail, newEmail)

            match change with
            | EmailChanged(oldValue, newValue) ->
                Assert.Equal(oldEmail, oldValue)
                Assert.Equal(newEmail, newValue)
            | _ -> Assert.True(false, "Expected EmailChanged")
        | Error _ -> Assert.True(false, "Failed to create new email")

    [<Fact>]
    let ``ProfilePicChanged stores old and new values correctly`` () =
        let oldValue = Some(createValidUrl ())

        let newValue =
            Some(
                Url.Create("https://yahoo.com")
                |> Result.defaultWith (fun _ -> failwith "Invalid url in test setup")
            )

        let change = ProfilePicChanged(oldValue, newValue)

        match change with
        | ProfilePicChanged(oldVal, newVal) ->
            Assert.Equal(oldValue, oldVal)
            Assert.Equal(newValue, newVal)
        | _ -> Assert.True(false, "Expected ProfilePicChanged")

    [<Fact>]
    let ``ProfilePicChanged handles None values correctly`` () =
        let newUrl = createValidUrl ()
        let change = ProfilePicChanged(None, Some(newUrl))

        match change with
        | ProfilePicChanged(oldVal, newVal) ->
            Assert.Equal(None, oldVal)
            Assert.Equal(Some newUrl, newVal)
        | _ -> Assert.True(false, "Expected ProfilePicChanged")

    [<Fact>]
    let ``Multiple event creations have unique EventIds`` () =
        let userId = createValidUserId ()
        let email = createValidEmail ()

        let event1 = UserEvents.userCreated userId "Name1" email None
        let event2 = UserEvents.userCreated userId "Name2" email None
        let event3 = UserEvents.userDeleted userId

        Assert.NotEqual(event1.EventId, event2.EventId)
        Assert.NotEqual(event1.EventId, event3.EventId)
        Assert.NotEqual(event2.EventId, event3.EventId)