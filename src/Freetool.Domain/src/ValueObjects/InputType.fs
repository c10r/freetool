namespace Freetool.Domain.ValueObjects

open Freetool.Domain

type InputTypeValue =
    | Email
    | Date
    | Text of maxLength: int
    | Integer
    | Boolean
    | MultiChoice of choices: InputTypeValue list

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

    static member MultiChoice(choices: InputTypeValue list) : Result<InputType, DomainError> =
        if List.isEmpty choices then
            Error(ValidationError "MultiChoice must have at least one choice")
        else
            // Validate that no choice is itself a MultiChoice
            let hasNestedMultiChoice =
                choices
                |> List.exists (function
                    | MultiChoice _ -> true
                    | _ -> false)

            if hasNestedMultiChoice then
                Error(ValidationError "MultiChoice cannot contain another MultiChoice")
            else
                Ok(InputType(MultiChoice(choices)))

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
        | MultiChoice choices ->
            let choiceStrings =
                choices
                |> List.map (fun c ->
                    match c with
                    | Email -> "Email"
                    | Date -> "Date"
                    | Text maxLength -> sprintf "Text(%d)" maxLength
                    | Integer -> "Integer"
                    | Boolean -> "Boolean"
                    | MultiChoice _ -> failwith "Should not be possible due to validation")

            sprintf "MultiChoice([%s])" (System.String.Join(", ", choiceStrings))