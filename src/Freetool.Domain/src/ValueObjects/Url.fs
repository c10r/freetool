namespace Freetool.Domain.ValueObjects

open System.Text.RegularExpressions
open Freetool.Domain

type Url =
    private
    | Url of string

    static member Create(url: string) : Result<Url, DomainError> =
        if System.String.IsNullOrWhiteSpace(url) then
            Error(ValidationError "URL cannot be empty")
        elif url.Length > 2_000 then
            Error(ValidationError "URL cannot exceed 2000 characters")
        elif not (Url.isValidFormat url) then
            Error(ValidationError "Invalid URL format")
        else
            Ok(Url url)

    member this.Value =
        let (Url url) = this
        url

    override this.ToString() = this.Value

    static member private isValidFormat(url: string) =
        let pattern =
            @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)"

        Regex.IsMatch(url, pattern)