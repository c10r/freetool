namespace Freetool.Application.Mappers

open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.DTOs

module ResourceMapper =
    let private keyValuePairFromDto (dto: KeyValuePairDto) : (string * string) = (dto.Key, dto.Value)

    let fromCreateDto (dto: CreateResourceDto) : Result<ValidatedResource, DomainError> =
        let urlParameters = dto.UrlParameters |> List.map keyValuePairFromDto
        let headers = dto.Headers |> List.map keyValuePairFromDto
        let body = dto.Body |> List.map keyValuePairFromDto

        Resource.create dto.Name dto.Description dto.BaseUrl urlParameters headers body dto.HttpMethod

    let fromUpdateNameDto
        (dto: UpdateResourceNameDto)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        Resource.updateName dto.Name resource

    let fromUpdateDescriptionDto
        (dto: UpdateResourceDescriptionDto)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        Resource.updateDescription dto.Description resource

    let fromUpdateBaseUrlDto
        (dto: UpdateResourceBaseUrlDto)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        Resource.updateBaseUrl dto.BaseUrl resource

    let fromUpdateUrlParametersDto
        (dto: UpdateResourceUrlParametersDto)
        (apps: AppResourceConflictData list)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        let urlParameters = dto.UrlParameters |> List.map keyValuePairFromDto
        Resource.updateUrlParameters urlParameters apps resource

    let fromUpdateHeadersDto
        (dto: UpdateResourceHeadersDto)
        (apps: AppResourceConflictData list)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        let headers = dto.Headers |> List.map keyValuePairFromDto
        Resource.updateHeaders headers apps resource

    let fromUpdateBodyDto
        (dto: UpdateResourceBodyDto)
        (apps: AppResourceConflictData list)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        let body = dto.Body |> List.map keyValuePairFromDto
        Resource.updateBody body apps resource