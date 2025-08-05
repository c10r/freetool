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
    | MultiChoice of Choices: InputTypeDto list