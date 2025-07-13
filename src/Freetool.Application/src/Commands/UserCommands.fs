namespace Freetool.Application.Commands

open Freetool.Application.DTOs

type UserCommandResult =
    | UserResult of UserDto
    | UsersResult of PagedUsersDto
    | UnitResult of unit

type UserCommand =
    | CreateUser of CreateUserDto
    | GetUserById of userId: string
    | GetUserByEmail of email: string
    | GetAllUsers of skip: int * take: int
    | DeleteUser of userId: string
    | UpdateUserName of userId: string * name: string
    | UpdateUserEmail of userId: string * email: string
    | SetProfilePicture of userId: string * profilePicUrl: string
    | RemoveProfilePicture of userId: string