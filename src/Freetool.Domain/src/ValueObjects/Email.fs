namespace Freetool.Domain.ValueObjects

open System.Text.RegularExpressions
open Freetool.Domain

type Email =
    private
    | Email of string

    static member Create(email: string) : Result<Email, DomainError> =
        if System.String.IsNullOrWhiteSpace(email) then
            Error(ValidationError "Email cannot be empty")
        elif email.Length > 254 then
            Error(ValidationError "Email cannot exceed 254 characters")
        elif not (Email.isValidFormat email) then
            Error(ValidationError "Invalid email format")
        else
            Ok(Email email)

    member this.Value =
        let (Email email) = this
        email

    override this.ToString() = this.Value

    static member private isValidFormat(email: string) =
        let pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"
        Regex.IsMatch(email, pattern)