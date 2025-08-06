namespace Freetool.Application.DTOs

type FolderLocation =
    | RootFolder
    | ChildFolder of parentId: string

type InputTypeDto =
    | Email
    | Date
    | Text of MaxLength: int
    | Integer
    | Boolean
    | MultiEmail of AllowedEmails: string list
    | MultiDate of AllowedDates: string list
    | MultiText of MaxLength: int * AllowedValues: string list
    | MultiInteger of AllowedIntegers: int list