namespace Freetool.Application.Commands

open Freetool.Domain.Entities
open Freetool.Application.DTOs

type ResourceCommandResult =
    | ResourceResult of ResourceDto
    | ResourcesResult of PagedResourcesDto
    | ResourceUnitResult of unit

type ResourceCommand =
    | CreateResource of ValidatedResource
    | GetResourceById of resourceId: string
    | GetAllResources of skip: int * take: int
    | DeleteResource of resourceId: string
    | UpdateResourceName of resourceId: string * UpdateResourceNameDto
    | UpdateResourceDescription of resourceId: string * UpdateResourceDescriptionDto
    | UpdateResourceBaseUrl of resourceId: string * UpdateResourceBaseUrlDto
    | UpdateResourceUrlParameters of resourceId: string * UpdateResourceUrlParametersDto
    | UpdateResourceHeaders of resourceId: string * UpdateResourceHeadersDto
    | UpdateResourceBody of resourceId: string * UpdateResourceBodyDto