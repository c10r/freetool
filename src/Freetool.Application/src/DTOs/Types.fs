namespace Freetool.Application.DTOs

type FolderLocation =
    | RootFolder
    | ChildFolder of parentId: string