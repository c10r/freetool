namespace Freetool.Domain.ValueObjects

open Freetool.Domain

[<Struct>]
type AppName =
    | AppName of string

    static member Create(name: string) : Result<AppName, DomainError> =
        if System.String.IsNullOrWhiteSpace(name) then
            Error(ValidationError "App name cannot be empty")
        elif name.Length > 100 then
            Error(ValidationError "App name cannot exceed 100 characters")
        else
            Ok(AppName(name.Trim()))

    member this.Value =
        let (AppName name) = this
        name

    override this.ToString() = this.Value