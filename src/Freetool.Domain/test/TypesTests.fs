namespace Freetool.Domain.Tests

open System
open Xunit
open Freetool.Domain

module TypesTests =

    [<Fact>]
    let ``DomainError ValidationError contains message`` () =
        let error = ValidationError "Test validation error"

        match error with
        | ValidationError msg -> Assert.Equal("Test validation error", msg)
        | _ -> Assert.True(false, "Expected ValidationError")

    [<Fact>]
    let ``DomainError NotFound contains message`` () =
        let error = NotFound "Resource not found"

        match error with
        | NotFound msg -> Assert.Equal("Resource not found", msg)
        | _ -> Assert.True(false, "Expected NotFound")

    [<Fact>]
    let ``DomainError Conflict contains message`` () =
        let error = Conflict "Resource already exists"

        match error with
        | Conflict msg -> Assert.Equal("Resource already exists", msg)
        | _ -> Assert.True(false, "Expected Conflict")

    [<Fact>]
    let ``DomainError InvalidOperation contains message`` () =
        let error = InvalidOperation "Operation not allowed"

        match error with
        | InvalidOperation msg -> Assert.Equal("Operation not allowed", msg)
        | _ -> Assert.True(false, "Expected InvalidOperation")

    [<Fact>]
    let ``DomainError cases are distinct`` () =
        let validationError = ValidationError "validation"
        let notFoundError = NotFound "not found"
        let conflictError = Conflict "conflict"
        let invalidOpError = InvalidOperation "invalid op"

        Assert.NotEqual(validationError, notFoundError)
        Assert.NotEqual(validationError, conflictError)
        Assert.NotEqual(validationError, invalidOpError)
        Assert.NotEqual(notFoundError, conflictError)
        Assert.NotEqual(notFoundError, invalidOpError)
        Assert.NotEqual(conflictError, invalidOpError)

    type TestEntity = {
        Id: int
        Name: string
    } with

        interface IEntity<int> with
            member this.Id = this.Id

    [<Fact>]
    let ``IEntity interface provides Id property`` () =
        let testEntity = { Id = 42; Name = "Test" }
        let entity = testEntity :> IEntity<int>

        Assert.Equal(42, entity.Id)

    type TestDomainEvent = {
        OccurredAt: DateTime
        EventId: Guid
        Data: string
    } with

        interface IDomainEvent with
            member this.OccurredAt = this.OccurredAt
            member this.EventId = this.EventId

    [<Fact>]
    let ``IDomainEvent interface provides OccurredAt and EventId properties`` () =
        let now = DateTime.UtcNow
        let eventId = Guid.NewGuid()

        let testEvent = {
            OccurredAt = now
            EventId = eventId
            Data = "test data"
        }

        let domainEvent = testEvent :> IDomainEvent

        Assert.Equal(now, domainEvent.OccurredAt)
        Assert.Equal(eventId, domainEvent.EventId)