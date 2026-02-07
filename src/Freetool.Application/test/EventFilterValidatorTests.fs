module Freetool.Application.Tests.EventFilterValidatorTests

open Xunit
open Freetool.Application.DTOs

[<Fact>]
let ``validate keeps large skip values`` () =
    let filterDto: EventFilterDTO =
        { UserId = None
          EventType = None
          EntityType = None
          FromDate = None
          ToDate = None
          Skip = Some 2000
          Take = Some 50 }

    let result = EventFilterValidator.validate filterDto

    match result with
    | Ok filter ->
        Assert.Equal(2000, filter.Skip)
        Assert.Equal(50, filter.Take)
    | Error errors -> failwith $"Expected Ok but got Error: {errors}"

[<Fact>]
let ``validate defaults skip and take when not provided`` () =
    let filterDto: EventFilterDTO =
        { UserId = None
          EventType = None
          EntityType = None
          FromDate = None
          ToDate = None
          Skip = None
          Take = None }

    let result = EventFilterValidator.validate filterDto

    match result with
    | Ok filter ->
        Assert.Equal(0, filter.Skip)
        Assert.Equal(50, filter.Take)
    | Error errors -> failwith $"Expected Ok but got Error: {errors}"

[<Fact>]
let ``validate rejects negative skip`` () =
    let filterDto: EventFilterDTO =
        { UserId = None
          EventType = None
          EntityType = None
          FromDate = None
          ToDate = None
          Skip = Some -1
          Take = Some 50 }

    let result = EventFilterValidator.validate filterDto

    match result with
    | Ok _ -> failwith "Expected Error but got Ok"
    | Error errors -> Assert.Contains("Skip must be greater than or equal to 0", errors)

[<Fact>]
let ``validate rejects take over max`` () =
    let filterDto: EventFilterDTO =
        { UserId = None
          EventType = None
          EntityType = None
          FromDate = None
          ToDate = None
          Skip = Some 0
          Take = Some 101 }

    let result = EventFilterValidator.validate filterDto

    match result with
    | Ok _ -> failwith "Expected Error but got Ok"
    | Error errors -> Assert.Contains("Take must be between 0 and 100", errors)
