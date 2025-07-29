namespace Freetool.Domain.ValueObjects

open Freetool.Domain

[<Struct>]
type InputTitle =
    | InputTitle of string

    static member Create(title: string) : Result<InputTitle, DomainError> =
        if System.String.IsNullOrWhiteSpace(title) then
            Error(ValidationError "Input title cannot be empty")
        else
            Ok(InputTitle(title.Trim()))

    member this.Value =
        let (InputTitle title) = this
        title

    override this.ToString() = this.Value