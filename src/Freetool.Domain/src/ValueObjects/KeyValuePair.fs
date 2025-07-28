namespace Freetool.Domain.ValueObjects

open Freetool.Domain

type KeyValuePair =
    private
    | KeyValuePair of key: string * value: string

    static member Create(key: string, value: string) : Result<KeyValuePair, DomainError> =
        if System.String.IsNullOrWhiteSpace(key) then
            Error(ValidationError "Key cannot be empty")
        elif key.Length > 100 then
            Error(ValidationError "Key cannot exceed 100 characters")
        elif System.String.IsNullOrEmpty(value) then
            Error(ValidationError "Value cannot be null")
        elif value.Length > 1000 then
            Error(ValidationError "Value cannot exceed 1000 characters")
        else
            Ok(KeyValuePair(key.Trim(), value))

    member this.Key =
        let (KeyValuePair(key, _)) = this
        key

    member this.Value =
        let (KeyValuePair(_, value)) = this
        value

    override this.ToString() = $"{this.Key}={this.Value}"