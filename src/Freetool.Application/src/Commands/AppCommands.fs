namespace Freetool.Application.Commands

open Freetool.Domain.Entities
open Freetool.Application.DTOs

type AppCommandResult =
    | AppResult of AppData
    | AppsResult of PagedResult<AppData>
    | AppUnitResult of unit

type AppCommand =
    | CreateApp of ValidatedApp
    | GetAppById of appId: string
    | GetAppsByFolderId of folderId: string * skip: int * take: int
    | GetAllApps of skip: int * take: int
    | DeleteApp of appId: string
    | UpdateAppName of appId: string * UpdateAppNameDto
    | UpdateAppInputs of appId: string * UpdateAppInputsDto