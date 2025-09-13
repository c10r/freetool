namespace Freetool.Domain.ValueObjects

open Freetool.Domain
open System

type InputTypeValue =
    | Email
    | Date
    | Text of maxLength: int
    | Integer
    | Boolean
    | MultiEmail of allowedEmails: Email list
    | MultiDate of allowedDates: DateTime list
    | MultiText of maxLength: int * allowedValues: string list
    | MultiInteger of allowedIntegers: int list

[<Struct>]
type InputType =
    | InputType of InputTypeValue

    static member Email() = InputType(Email)
    static member Date() = InputType(Date)

    static member Text(maxLength: int) =
        if maxLength <= 0 || maxLength > 500 then
            Error(ValidationError "Text input max length must be between 1 and 500 characters")
        else
            Ok(InputType(Text(maxLength)))

    static member Integer() = InputType(Integer)
    static member Boolean() = InputType(Boolean)

    static member MultiEmail(allowedEmails: Email list) : Result<InputType, DomainError> =
        if List.isEmpty allowedEmails then
            Error(ValidationError "MultiEmail must have at least one allowed email")
        else
            Ok(InputType(MultiEmail(allowedEmails)))

    static member MultiDate(allowedDates: DateTime list) : Result<InputType, DomainError> =
        if List.isEmpty allowedDates then
            Error(ValidationError "MultiDate must have at least one allowed date")
        else
            Ok(InputType(MultiDate(allowedDates)))

    static member MultiText(maxLength: int, allowedValues: string list) : Result<InputType, DomainError> =
        if maxLength <= 0 || maxLength > 500 then
            Error(ValidationError "MultiText max length must be between 1 and 500 characters")
        elif List.isEmpty allowedValues then
            Error(ValidationError "MultiText must have at least one allowed value")
        elif allowedValues |> List.exists (fun v -> v.Length > maxLength) then
            Error(ValidationError "All MultiText allowed values must not exceed max length")
        else
            Ok(InputType(MultiText(maxLength, allowedValues)))

    static member MultiInteger(allowedIntegers: int list) : Result<InputType, DomainError> =
        if List.isEmpty allowedIntegers then
            Error(ValidationError "MultiInteger must have at least one allowed integer")
        else
            Ok(InputType(MultiInteger(allowedIntegers)))

    member this.Value =
        let (InputType inputType) = this
        inputType

    override this.ToString() =
        match this.Value with
        | Email -> "Email"
        | Date -> "Date"
        | Text maxLength -> sprintf "Text(%d)" maxLength
        | Integer -> "Integer"
        | Boolean -> "Boolean"
        | MultiEmail allowedEmails ->
            let emailStrings = allowedEmails |> List.map (fun e -> e.ToString())
            sprintf "MultiEmail([%s])" (String.Join(", ", emailStrings))
        | MultiDate allowedDates ->
            let dateStrings = allowedDates |> List.map (fun d -> d.ToString("yyyy-MM-dd"))
            sprintf "MultiDate([%s])" (String.Join(", ", dateStrings))
        | MultiText(maxLength, allowedValues) ->
            sprintf "MultiText(%d, [%s])" maxLength (String.Join(", ", allowedValues))
        | MultiInteger allowedIntegers ->
            let intStrings = allowedIntegers |> List.map string
            sprintf "MultiInteger([%s])" (String.Join(", ", intStrings))
