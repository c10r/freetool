namespace Freetool.Application.Mappers

open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.DTOs

module ResourceMapper =
    let private keyValuePairFromDto (dto: KeyValuePairDto) : (string * string) = (dto.Key, dto.Value)

    let fromCreateDto (actorUserId: UserId) (dto: CreateResourceDto) : Result<ValidatedResource, DomainError> =
        let urlParameters = dto.UrlParameters |> List.map keyValuePairFromDto
        let headers = dto.Headers |> List.map keyValuePairFromDto
        let body = dto.Body |> List.map keyValuePairFromDto

        Resource.create actorUserId dto.Name dto.Description dto.BaseUrl urlParameters headers body dto.HttpMethod

    let fromUpdateNameDto
        (actorUserId: UserId)
        (dto: UpdateResourceNameDto)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        Resource.updateName actorUserId dto.Name resource

    let fromUpdateDescriptionDto
        (actorUserId: UserId)
        (dto: UpdateResourceDescriptionDto)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        Resource.updateDescription actorUserId dto.Description resource

    let fromUpdateBaseUrlDto
        (actorUserId: UserId)
        (dto: UpdateResourceBaseUrlDto)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        Resource.updateBaseUrl actorUserId dto.BaseUrl resource

    let fromUpdateUrlParametersDto
        (actorUserId: UserId)
        (dto: UpdateResourceUrlParametersDto)
        (apps: AppResourceConflictData list)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        let urlParameters = dto.UrlParameters |> List.map keyValuePairFromDto
        Resource.updateUrlParameters actorUserId urlParameters apps resource

    let fromUpdateHeadersDto
        (actorUserId: UserId)
        (dto: UpdateResourceHeadersDto)
        (apps: AppResourceConflictData list)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        let headers = dto.Headers |> List.map keyValuePairFromDto
        Resource.updateHeaders actorUserId headers apps resource

    let fromUpdateBodyDto
        (actorUserId: UserId)
        (dto: UpdateResourceBodyDto)
        (apps: AppResourceConflictData list)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        let body = dto.Body |> List.map keyValuePairFromDto
        Resource.updateBody actorUserId body apps resource