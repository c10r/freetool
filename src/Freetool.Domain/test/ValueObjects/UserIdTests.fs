namespace Freetool.Domain.Tests.ValueObjects

open System
open Xunit
open Freetool.Domain.ValueObjects

module UserIdTests =

    [<Fact>]
    let ``NewId creates a UserId with a new GUID`` () =
        let userId1 = UserId.NewId()
        let userId2 = UserId.NewId()

        Assert.NotEqual(userId1, userId2)
        Assert.NotEqual(Guid.Empty, userId1.Value)
        Assert.NotEqual(Guid.Empty, userId2.Value)

    [<Fact>]
    let ``FromGuid creates a UserId from the provided GUID`` () =
        let expectedGuid = Guid.NewGuid()
        let userId = UserId.FromGuid(expectedGuid)

        Assert.Equal(expectedGuid, userId.Value)

    [<Fact>]
    let ``Value property returns the underlying GUID`` () =
        let expectedGuid = Guid.NewGuid()
        let userId = UserId.FromGuid(expectedGuid)

        Assert.Equal(expectedGuid, userId.Value)

    [<Fact>]
    let ``ToString returns the GUID string representation`` () =
        let guid = Guid.NewGuid()
        let userId = UserId.FromGuid(guid)

        Assert.Equal(guid.ToString(), userId.ToString())

    [<Fact>]
    let ``Two UserIds with same GUID are equal`` () =
        let guid = Guid.NewGuid()
        let userId1 = UserId.FromGuid(guid)
        let userId2 = UserId.FromGuid(guid)

        Assert.Equal(userId1, userId2)

    [<Fact>]
    let ``Two UserIds with different GUIDs are not equal`` () =
        let userId1 = UserId.NewId()
        let userId2 = UserId.NewId()

        Assert.NotEqual(userId1, userId2)