namespace Freetool.Domain.Events

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects

type ResourceChange =
    | NameChanged of oldValue: ResourceName * newValue: ResourceName
    | DescriptionChanged of oldValue: ResourceDescription * newValue: ResourceDescription
    | BaseUrlChanged of oldValue: BaseUrl * newValue: BaseUrl
    | UrlParametersChanged of oldValue: KeyValuePair list * newValue: KeyValuePair list
    | HeadersChanged of oldValue: KeyValuePair list * newValue: KeyValuePair list
    | BodyChanged of oldValue: KeyValuePair list * newValue: KeyValuePair list

type ResourceCreatedEvent = {
    ResourceId: ResourceId
    Name: ResourceName
    Description: ResourceDescription
    BaseUrl: BaseUrl
    UrlParameters: KeyValuePair list
    Headers: KeyValuePair list
    Body: KeyValuePair list
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

type ResourceUpdatedEvent = {
    ResourceId: ResourceId
    Changes: ResourceChange list
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

type ResourceDeletedEvent = {
    ResourceId: ResourceId
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

module ResourceEvents =
    let resourceCreated
        (resourceId: ResourceId)
        (name: ResourceName)
        (description: ResourceDescription)
        (baseUrl: BaseUrl)
        (urlParameters: KeyValuePair list)
        (headers: KeyValuePair list)
        (body: KeyValuePair list)
        =
        {
            ResourceId = resourceId
            Name = name
            Description = description
            BaseUrl = baseUrl
            UrlParameters = urlParameters
            Headers = headers
            Body = body
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }
        : ResourceCreatedEvent

    let resourceUpdated (resourceId: ResourceId) (changes: ResourceChange list) =
        {
            ResourceId = resourceId
            Changes = changes
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }
        : ResourceUpdatedEvent

    let resourceDeleted (resourceId: ResourceId) =
        {
            ResourceId = resourceId
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }
        : ResourceDeletedEvent