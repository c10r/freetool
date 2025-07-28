namespace Freetool.Domain.ValueObjects

open Freetool.Domain

type ResourceName =
    private
    | ResourceName of string

    static member Create(name: string) : Result<ResourceName, DomainError> =
        if System.String.IsNullOrWhiteSpace(name) then
            Error(ValidationError "Resource name cannot be empty")
        elif name.Length > 100 then
            Error(ValidationError "Resource name cannot exceed 100 characters")
        else
            Ok(ResourceName(name.Trim()))

    member this.Value =
        let (ResourceName name) = this
        name

    override this.ToString() = this.Value