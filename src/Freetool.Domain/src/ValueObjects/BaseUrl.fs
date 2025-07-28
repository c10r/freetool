namespace Freetool.Domain.ValueObjects

open System.Text.RegularExpressions
open Freetool.Domain

type BaseUrl =
    private
    | BaseUrl of string

    static member Create(url: string) : Result<BaseUrl, DomainError> =
        if System.String.IsNullOrWhiteSpace(url) then
            Error(ValidationError "Base URL cannot be empty")
        elif url.Length > 1000 then
            Error(ValidationError "Base URL cannot exceed 1000 characters")
        elif not (BaseUrl.isValidFormat url) then
            Error(ValidationError "Invalid URL format")
        else
            Ok(BaseUrl(url.Trim()))

    member this.Value =
        let (BaseUrl url) = this
        url

    override this.ToString() = this.Value

    static member private isValidFormat(url: string) =
        let pattern = @"^https?://[^\s/$.?#].[^\s]*$"
        Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase)