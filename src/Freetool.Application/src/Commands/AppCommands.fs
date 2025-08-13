namespace Freetool.Application.Commands

open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.DTOs

type AppCommandResult =
    | AppResult of AppData
    | AppsResult of PagedResult<AppData>
    | AppUnitResult of unit

type AppCommand =
    | CreateApp of actorUserId: UserId * ValidatedApp
    | GetAppById of appId: string
    | GetAppsByFolderId of folderId: string * skip: int * take: int
    | GetAllApps of skip: int * take: int
    | DeleteApp of actorUserId: UserId * appId: string
    | UpdateAppName of actorUserId: UserId * appId: string * UpdateAppNameDto
    | UpdateAppInputs of actorUserId: UserId * appId: string * UpdateAppInputsDto
    | UpdateAppQueryParameters of actorUserId: UserId * appId: string * UpdateAppQueryParametersDto
    | UpdateAppBody of actorUserId: UserId * appId: string * UpdateAppBodyDto
    | UpdateAppHeaders of actorUserId: UserId * appId: string * UpdateAppHeadersDto