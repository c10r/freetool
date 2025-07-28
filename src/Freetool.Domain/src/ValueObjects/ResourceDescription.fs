namespace Freetool.Domain.ValueObjects

open Freetool.Domain

type ResourceDescription =
    private
    | ResourceDescription of string

    static member Create(description: string) : Result<ResourceDescription, DomainError> =
        if System.String.IsNullOrWhiteSpace(description) then
            Error(ValidationError "Resource description cannot be empty")
        elif description.Length > 500 then
            Error(ValidationError "Resource description cannot exceed 500 characters")
        else
            Ok(ResourceDescription(description.Trim()))

    member this.Value =
        let (ResourceDescription description) = this
        description

    override this.ToString() = this.Value