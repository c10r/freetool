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
    | HttpMethodChanged of oldValue: HttpMethod * newValue: HttpMethod

type ResourceCreatedEvent =
    { ResourceId: ResourceId
      Name: ResourceName
      Description: ResourceDescription
      SpaceId: SpaceId
      HttpMethod: HttpMethod
      BaseUrl: BaseUrl
      UrlParameters: KeyValuePair list
      Headers: KeyValuePair list
      Body: KeyValuePair list
      OccurredAt: DateTime
      EventId: Guid
      ActorUserId: UserId }

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId
        member this.UserId = this.ActorUserId

type ResourceUpdatedEvent =
    { ResourceId: ResourceId
      Changes: ResourceChange list
      OccurredAt: DateTime
      EventId: Guid
      ActorUserId: UserId }

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId
        member this.UserId = this.ActorUserId

type ResourceDeletedEvent =
    { ResourceId: ResourceId
      Name: ResourceName
      OccurredAt: DateTime
      EventId: Guid
      ActorUserId: UserId }

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId
        member this.UserId = this.ActorUserId

module ResourceEvents =
    let resourceCreated
        (actorUserId: UserId)
        (resourceId: ResourceId)
        (name: ResourceName)
        (description: ResourceDescription)
        (spaceId: SpaceId)
        (baseUrl: BaseUrl)
        (urlParameters: KeyValuePair list)
        (headers: KeyValuePair list)
        (body: KeyValuePair list)
        (httpMethod: HttpMethod)
        =
        { ResourceId = resourceId
          Name = name
          Description = description
          SpaceId = spaceId
          HttpMethod = httpMethod
          BaseUrl = baseUrl
          UrlParameters = urlParameters
          Headers = headers
          Body = body
          OccurredAt = DateTime.UtcNow
          EventId = Guid.NewGuid()
          ActorUserId = actorUserId }
        : ResourceCreatedEvent

    let resourceUpdated (actorUserId: UserId) (resourceId: ResourceId) (changes: ResourceChange list) =
        { ResourceId = resourceId
          Changes = changes
          OccurredAt = DateTime.UtcNow
          EventId = Guid.NewGuid()
          ActorUserId = actorUserId }
        : ResourceUpdatedEvent

    let resourceDeleted (actorUserId: UserId) (resourceId: ResourceId) (name: ResourceName) =
        { ResourceId = resourceId
          Name = name
          OccurredAt = DateTime.UtcNow
          EventId = Guid.NewGuid()
          ActorUserId = actorUserId }
        : ResourceDeletedEvent
