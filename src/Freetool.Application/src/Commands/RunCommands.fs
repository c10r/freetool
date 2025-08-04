namespace Freetool.Application.Commands

open Freetool.Domain.Entities
open Freetool.Application.DTOs

type RunCommandResult =
    | RunResult of RunDto
    | RunsResult of PagedRunsDto
    | RunUnitResult of unit

type RunCommand =
    | CreateRun of appId: string * CreateRunDto
    | GetRunById of runId: string
    | GetRunsByAppId of appId: string * skip: int * take: int
    | GetRunsByStatus of status: string * skip: int * take: int
    | GetRunsByAppIdAndStatus of appId: string * status: string * skip: int * take: int