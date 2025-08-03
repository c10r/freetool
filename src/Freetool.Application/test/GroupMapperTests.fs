module Freetool.Domain.Tests.GroupMapperTests

open System
open Xunit
open Freetool.Application.DTOs
open Freetool.Application.Mappers

[<Fact>]
let ``GroupMapper fromCreateDto with no UserIds should create group with empty user list`` () =
    // Arrange
    let dto = { Name = "Test Group"; UserIds = None }

    // Act
    let result = GroupMapper.fromCreateDto dto

    // Assert
    Assert.Equal("Test Group", result.State.Name)
    Assert.Empty(result.State.UserIds)
    Assert.Empty(result.UncommittedEvents)

[<Fact>]
let ``GroupMapper fromCreateDto with empty UserIds list should create group with empty user list`` () =
    // Arrange
    let dto = {
        Name = "Test Group"
        UserIds = Some []
    }

    // Act
    let result = GroupMapper.fromCreateDto dto

    // Assert
    Assert.Equal("Test Group", result.State.Name)
    Assert.Empty(result.State.UserIds)

[<Fact>]
let ``GroupMapper fromCreateDto with valid UserIds should create group with users`` () =
    // Arrange
    let user1Guid = Guid.NewGuid()
    let user2Guid = Guid.NewGuid()

    let dto = {
        Name = "Test Group"
        UserIds = Some [ user1Guid.ToString(); user2Guid.ToString() ]
    }

    // Act
    let result = GroupMapper.fromCreateDto dto

    // Assert
    Assert.Equal("Test Group", result.State.Name)
    Assert.Equal(2, result.State.UserIds.Length)

    let userIdValues = result.State.UserIds |> List.map (fun uid -> uid.Value)
    Assert.Contains(user1Guid, userIdValues)
    Assert.Contains(user2Guid, userIdValues)

[<Fact>]
let ``GroupMapper fromCreateDto with invalid UserIds should filter out invalid GUIDs`` () =
    // Arrange
    let validGuid = Guid.NewGuid()

    let dto = {
        Name = "Test Group"
        UserIds = Some [ validGuid.ToString(); "invalid-guid"; "not-a-guid"; "" ]
    }

    // Act
    let result = GroupMapper.fromCreateDto dto

    // Assert
    Assert.Equal("Test Group", result.State.Name)
    Assert.Single(result.State.UserIds) |> ignore
    Assert.Equal(validGuid, result.State.UserIds.[0].Value)

[<Fact>]
let ``GroupMapper fromCreateDto with duplicate UserIds should remove duplicates`` () =
    // Arrange
    let user1Guid = Guid.NewGuid()
    let user2Guid = Guid.NewGuid()

    let dto = {
        Name = "Test Group"
        UserIds =
            Some [
                user1Guid.ToString()
                user2Guid.ToString()
                user1Guid.ToString() // Duplicate
                user2Guid.ToString() // Duplicate
            ]
    }

    // Act
    let result = GroupMapper.fromCreateDto dto

    // Assert
    Assert.Equal("Test Group", result.State.Name)
    Assert.Equal(2, result.State.UserIds.Length) // Should have only 2 unique users

    let userIdValues = result.State.UserIds |> List.map (fun uid -> uid.Value)
    Assert.Contains(user1Guid, userIdValues)
    Assert.Contains(user2Guid, userIdValues)

[<Fact>]
let ``GroupMapper fromCreateDto with mixed valid and invalid UserIds should only include valid ones`` () =
    // Arrange
    let validGuid1 = Guid.NewGuid()
    let validGuid2 = Guid.NewGuid()

    let dto = {
        Name = "Test Group"
        UserIds =
            Some [
                validGuid1.ToString()
                "invalid-guid-1"
                validGuid2.ToString()
                "not-a-guid"
                validGuid1.ToString() // Duplicate valid
            ]
    }

    // Act
    let result = GroupMapper.fromCreateDto dto

    // Assert
    Assert.Equal("Test Group", result.State.Name)
    Assert.Equal(2, result.State.UserIds.Length) // Should have only 2 unique valid users

    let userIdValues = result.State.UserIds |> List.map (fun uid -> uid.Value)
    Assert.Contains(validGuid1, userIdValues)
    Assert.Contains(validGuid2, userIdValues)