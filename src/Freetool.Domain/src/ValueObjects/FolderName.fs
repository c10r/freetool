namespace Freetool.Domain.ValueObjects

open Freetool.Domain

type FolderName =
    private
    | FolderName of string

    static member Create(name: string) : Result<FolderName, DomainError> =
        if System.String.IsNullOrWhiteSpace(name) then
            Error(ValidationError "Folder name cannot be empty")
        elif name.Length > 100 then
            Error(ValidationError "Folder name cannot exceed 100 characters")
        else
            Ok(FolderName(name.Trim()))

    member this.Value =
        let (FolderName name) = this
        name

    override this.ToString() = this.Value