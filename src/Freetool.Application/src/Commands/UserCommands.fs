namespace Freetool.Application.Commands

open Freetool.Domain.Entities
open Freetool.Application.DTOs

type UserCommandResult =
    | UserResult of UserDto
    | UsersResult of PagedUsersDto
    | UnitResult of unit

type UserCommand =
    | CreateUser of ValidatedUser
    | GetUserById of userId: string
    | GetUserByEmail of email: string
    | GetAllUsers of skip: int * take: int
    | DeleteUser of userId: string
    | UpdateUserName of userId: string * UpdateUserNameDto
    | UpdateUserEmail of userId: string * UpdateUserEmailDto
    | SetProfilePicture of userId: string * SetProfilePictureDto
    | RemoveProfilePicture of userId: string