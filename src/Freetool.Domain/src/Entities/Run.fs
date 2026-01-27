namespace Freetool.Domain.Entities

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open System.Text.Json.Serialization
open System.Text.RegularExpressions
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events
open Freetool.Domain.Services

[<Table("Runs")>]
// CLIMutable for EntityFramework
[<CLIMutable>]
type RunData =
    { [<Key>]
      Id: RunId

      [<Required>]
      AppId: AppId

      [<Required>]
      [<MaxLength(50)>]
      Status: RunStatus

      [<Required>]
      [<Column(TypeName = "TEXT")>] // JSON serialized list of RunInputValue
      InputValues: RunInputValue list

      [<Column(TypeName = "TEXT")>] // JSON serialized ExecutableHttpRequest (null until composed)
      ExecutableRequest: ExecutableHttpRequest option

      [<Column(TypeName = "TEXT")>] // HTTP response body (null until completed successfully)
      Response: string option

      [<Column(TypeName = "TEXT")>] // Error message (null unless failed)
      ErrorMessage: string option

      StartedAt: DateTime option // When the run was started (null if not started)

      CompletedAt: DateTime option // When the run was completed (null if not completed)

      [<Required>]
      [<JsonIgnore>]
      CreatedAt: DateTime

      [<JsonIgnore>]
      IsDeleted: bool }

type Run = EventSourcingAggregate<RunData>

module RunAggregateHelpers =
    let getEntityId (run: Run) : RunId = run.State.Id

    let implementsIEntity (run: Run) =
        { new IEntity<RunId> with
            member _.Id = run.State.Id }

// Type aliases for clarity
type UnvalidatedRun = Run // From DTOs - potentially unsafe
type ValidatedRun = Run // Validated domain model and database data

/// Represents the current user for variable substitution in templates
type CurrentUser =
    { Id: string
      Email: string
      FirstName: string
      LastName: string }

module Run =
    let fromData (runData: RunData) : ValidatedRun =
        { State = runData
          UncommittedEvents = [] }

    /// Validate dynamic body key-value pairs
    let validateDynamicBody (body: (string * string) list) : Result<(string * string) list, DomainError> =
        // Check max 10 keys
        if body.Length > 10 then
            Error(ValidationError "Dynamic body cannot have more than 10 key-value pairs")
        else
            // Check for empty keys
            let emptyKeys = body |> List.filter (fun (k, _) -> String.IsNullOrWhiteSpace k)

            if not emptyKeys.IsEmpty then
                Error(ValidationError "Dynamic body keys cannot be empty")
            else
                // Check for duplicate keys
                let keys = body |> List.map fst
                let uniqueKeys = keys |> Set.ofList

                if keys.Length <> uniqueKeys.Count then
                    let duplicates =
                        keys
                        |> List.groupBy id
                        |> List.filter (fun (_, group) -> group.Length > 1)
                        |> List.map fst

                    let duplicateList = String.concat ", " duplicates
                    Error(ValidationError $"Dynamic body cannot have duplicate keys: {duplicateList}")
                else
                    Ok body

    // Validate input values against app's input schema
    let private validateInputValues
        (appInputs: Input list)
        (inputValues: RunInputValue list)
        : Result<RunInputValue list, DomainError> =
        // Create a map of app inputs by title for quick lookup
        let appInputsMap =
            appInputs |> List.map (fun input -> input.Title, input) |> Map.ofList

        let inputValuesMap =
            inputValues |> List.map (fun iv -> iv.Title, iv.Value) |> Map.ofList

        // Apply default values for optional inputs that weren't provided
        let inputValuesWithDefaults =
            appInputs
            |> List.choose (fun input ->
                // If input wasn't provided and has a default, use it
                if
                    not input.Required
                    && not (inputValuesMap.ContainsKey input.Title)
                    && input.DefaultValue.IsSome
                then
                    Some
                        { Title = input.Title
                          Value = input.DefaultValue.Value.ToRawString() }
                else
                    None)
            |> List.append inputValues

        // Rebuild the map with defaults included
        let inputValuesMapWithDefaults =
            inputValuesWithDefaults |> List.map (fun iv -> iv.Title, iv.Value) |> Map.ofList

        // Check that all required inputs are provided
        let missingRequiredInputs =
            appInputs
            |> List.filter (fun input -> input.Required && not (inputValuesMapWithDefaults.ContainsKey input.Title))
            |> List.map (fun input -> input.Title)

        if not missingRequiredInputs.IsEmpty then
            let missingList = String.concat ", " missingRequiredInputs
            Error(ValidationError $"Missing required inputs: {missingList}")
        else
            // Check that all provided inputs exist in the app schema
            let invalidInputs =
                inputValuesWithDefaults
                |> List.filter (fun iv -> not (appInputsMap.ContainsKey iv.Title))
                |> List.map (fun iv -> iv.Title)

            if not invalidInputs.IsEmpty then
                let invalidList = String.concat ", " invalidInputs
                Error(ValidationError $"Invalid inputs not defined in app: {invalidList}")
            else
                // Validate each input value against its type
                let validateInputValue (inputValue: RunInputValue) (input: Input) : Result<RunInputValue, DomainError> =
                    let value = inputValue.Value

                    match input.Type.Value with
                    | Email ->
                        match Email.Create(Some value) with
                        | Ok _ -> Ok inputValue
                        | Error err -> Error err
                    | Integer ->
                        match System.Int32.TryParse(value) with
                        | true, _ -> Ok inputValue
                        | false, _ -> Error(ValidationError $"Input '{input.Title}' must be a valid integer")
                    | Boolean ->
                        match System.Boolean.TryParse(value) with
                        | true, _ -> Ok inputValue
                        | false, _ ->
                            Error(ValidationError $"Input '{input.Title}' must be a valid boolean (true/false)")
                    | Date ->
                        match System.DateTime.TryParse(value) with
                        | true, _ -> Ok inputValue
                        | false, _ -> Error(ValidationError $"Input '{input.Title}' must be a valid date")
                    | Text maxLength ->
                        if value.Length > maxLength then
                            Error(
                                ValidationError
                                    $"Input '{input.Title}' exceeds maximum length of {maxLength} characters"
                            )
                        else
                            Ok inputValue
                    | MultiEmail allowedEmails ->
                        match Email.Create(Some value) with
                        | Ok email ->
                            if
                                allowedEmails
                                |> List.exists (fun allowedEmail -> allowedEmail.ToString() = email.ToString())
                            then
                                Ok inputValue
                            else
                                Error(
                                    ValidationError $"Input '{input.Title}' must be one of the allowed email addresses"
                                )
                        | Error err -> Error err
                    | MultiDate allowedDates ->
                        match System.DateTime.TryParse(value) with
                        | true, date ->
                            if allowedDates |> List.exists (fun allowedDate -> allowedDate.Date = date.Date) then
                                Ok inputValue
                            else
                                Error(ValidationError $"Input '{input.Title}' must be one of the allowed dates")
                        | false, _ -> Error(ValidationError $"Input '{input.Title}' must be a valid date")
                    | MultiText(maxLength, allowedValues) ->
                        if value.Length > maxLength then
                            Error(
                                ValidationError
                                    $"Input '{input.Title}' exceeds maximum length of {maxLength} characters"
                            )
                        elif allowedValues |> List.exists (fun allowedValue -> allowedValue = value) then
                            Ok inputValue
                        else
                            Error(ValidationError $"Input '{input.Title}' must be one of the allowed text values")
                    | MultiInteger allowedIntegers ->
                        match System.Int32.TryParse(value) with
                        | true, intValue ->
                            if allowedIntegers |> List.exists (fun allowedInt -> allowedInt = intValue) then
                                Ok inputValue
                            else
                                Error(
                                    ValidationError $"Input '{input.Title}' must be one of the allowed integer values"
                                )
                        | false, _ -> Error(ValidationError $"Input '{input.Title}' must be a valid integer")
                    | Radio options ->
                        if options |> List.exists (fun o -> o.Value = value) then
                            Ok inputValue
                        else
                            Error(ValidationError $"Input '{input.Title}' must be one of the allowed radio options")

                // Validate all input values against their types
                let zippedInputs =
                    inputValuesWithDefaults
                    |> List.map (fun (value: RunInputValue) ->
                        match appInputsMap.TryFind value.Title with
                        | Some input -> Some(value, input)
                        | _ -> None)
                    |> List.choose id

                zippedInputs
                |> List.map (fun (input, inputDef) -> validateInputValue input inputDef)
                |> List.fold
                    (fun acc result ->
                        match acc, result with
                        | Ok xs, Ok x -> Ok(xs @ [ x ])
                        | Ok _, Error e -> Error e
                        | Error e, _ -> Error e)
                    (Ok [])

    let createWithValidation
        (actorUserId: UserId)
        (app: ValidatedApp)
        (inputValues: RunInputValue list)
        : Result<ValidatedRun, DomainError> =

        let appInputs = App.getInputs app

        match validateInputValues appInputs inputValues with
        | Error err -> Error err
        | Ok validatedInputValues ->
            let runData =
                { Id = RunId.NewId()
                  AppId = App.getId app
                  Status = Pending
                  InputValues = validatedInputValues
                  ExecutableRequest = None
                  Response = None
                  ErrorMessage = None
                  StartedAt = None
                  CompletedAt = None
                  CreatedAt = DateTime.UtcNow
                  IsDeleted = false }

            let runCreatedEvent =
                RunEvents.runCreated actorUserId runData.Id runData.AppId validatedInputValues

            Ok
                { State = runData
                  UncommittedEvents = [ runCreatedEvent :> IDomainEvent ] }

    let setExecutableRequest (executableRequest: ExecutableHttpRequest) (run: ValidatedRun) : ValidatedRun =
        let updatedRunData =
            { run.State with
                ExecutableRequest = Some executableRequest }

        { State = updatedRunData
          UncommittedEvents = run.UncommittedEvents }

    let composeExecutableRequestFromAppAndResource
        (run: ValidatedRun)
        (app: ValidatedApp)
        (resource: ValidatedResource)
        (currentUser: CurrentUser)
        (dynamicBody: (string * string) list option)
        : Result<ValidatedRun, DomainError> =

        let useDynamicJsonBody = App.getUseDynamicJsonBody app

        // Use existing RequestComposer to create the base ExecutableHttpRequest
        match RequestComposer.composeExecutableRequest resource app with
        | Error err -> Error err
        | Ok baseRequest ->
            // Substitute input values into the request
            let inputValuesMap =
                run.State.InputValues |> List.map (fun iv -> iv.Title, iv.Value) |> Map.ofList

            // Regex patterns for variable and expression matching
            // Match @"quoted name" or @identifier (with optional dot notation for current_user)
            let variablePattern =
                @"@(?:""([^""]+)""|([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)?))"

            // Match {{ expression }}
            let expressionPattern = @"\{\{([\s\S]*?)\}\}"

            /// Substitute variables in text (@var or @"var" syntax)
            let substituteVariables (text: string) : string =
                Regex.Replace(
                    text,
                    variablePattern,
                    fun m ->
                        // Group 1 is quoted name, Group 2 is unquoted identifier
                        let varName =
                            if m.Groups.[1].Success then
                                m.Groups.[1].Value
                            else
                                m.Groups.[2].Value

                        // Check for current_user prefix
                        if varName.StartsWith("current_user.") then
                            match varName with
                            | "current_user.email" -> currentUser.Email
                            | "current_user.id" -> currentUser.Id
                            | "current_user.firstName" -> currentUser.FirstName
                            | "current_user.lastName" -> currentUser.LastName
                            | _ -> m.Value // Keep original if not recognized
                        else
                            match inputValuesMap.TryFind varName with
                            | Some value -> value
                            | None -> m.Value // Keep original if not found
                )

            /// Check if a value is "truthy" for ternary evaluation
            let isTruthy (value: string) : bool =
                match value.Trim().ToLowerInvariant() with
                | ""
                | "0"
                | "false"
                | "null"
                | "undefined" -> false
                | _ -> true

            /// Try to parse a string as a decimal for arithmetic
            let tryParseDecimal (s: string) : decimal option =
                match System.Decimal.TryParse(s.Trim()) with
                | true, d -> Some d
                | _ -> None

            /// Evaluate a simple arithmetic expression (single binary operation)
            /// Supports: +, -, *, / with decimal numbers
            let evaluateArithmetic (expr: string) : string =
                let expr = expr.Trim()

                // Try to parse as a plain number first
                match tryParseDecimal expr with
                | Some d -> string d
                | None ->
                    // Try multiplication: a * b
                    let mulPattern = @"^\s*(-?\d+(?:\.\d+)?)\s*\*\s*(-?\d+(?:\.\d+)?)\s*$"
                    let mulMatch = Regex.Match(expr, mulPattern)

                    if mulMatch.Success then
                        match tryParseDecimal mulMatch.Groups.[1].Value, tryParseDecimal mulMatch.Groups.[2].Value with
                        | Some a, Some b -> string (a * b)
                        | _ -> expr
                    else
                        // Try division: a / b
                        let divPattern = @"^\s*(-?\d+(?:\.\d+)?)\s*/\s*(-?\d+(?:\.\d+)?)\s*$"
                        let divMatch = Regex.Match(expr, divPattern)

                        if divMatch.Success then
                            match
                                tryParseDecimal divMatch.Groups.[1].Value, tryParseDecimal divMatch.Groups.[2].Value
                            with
                            | Some a, Some b when b <> 0m -> string (a / b)
                            | _ -> expr
                        else
                            // Try addition: a + b
                            let addPattern = @"^\s*(-?\d+(?:\.\d+)?)\s*\+\s*(-?\d+(?:\.\d+)?)\s*$"
                            let addMatch = Regex.Match(expr, addPattern)

                            if addMatch.Success then
                                match
                                    tryParseDecimal addMatch.Groups.[1].Value, tryParseDecimal addMatch.Groups.[2].Value
                                with
                                | Some a, Some b -> string (a + b)
                                | _ -> expr
                            else
                                // Try subtraction: a - b (careful with negative first operand)
                                let subPattern = @"^\s*(-?\d+(?:\.\d+)?)\s*-\s*(\d+(?:\.\d+)?)\s*$"
                                let subMatch = Regex.Match(expr, subPattern)

                                if subMatch.Success then
                                    match
                                        tryParseDecimal subMatch.Groups.[1].Value,
                                        tryParseDecimal subMatch.Groups.[2].Value
                                    with
                                    | Some a, Some b -> string (a - b)
                                    | _ -> expr
                                else
                                    expr // Return original if no pattern matches

            /// Evaluate a simple expression (supports variables, arithmetic, ternary)
            let evaluateExpression (expr: string) : string =
                // First substitute all variables
                let substituted = substituteVariables expr

                // Check for ternary: condition ? trueVal : falseVal
                let ternaryPattern = @"^\s*(.+?)\s*\?\s*(.+?)\s*:\s*(.+?)\s*$"
                let ternaryMatch = Regex.Match(substituted, ternaryPattern)

                if ternaryMatch.Success then
                    let condition = ternaryMatch.Groups.[1].Value.Trim()
                    let trueVal = ternaryMatch.Groups.[2].Value.Trim()
                    let falseVal = ternaryMatch.Groups.[3].Value.Trim()

                    // Evaluate condition (truthy check)
                    if isTruthy condition then
                        evaluateArithmetic trueVal
                    else
                        evaluateArithmetic falseVal
                else
                    evaluateArithmetic substituted

            /// Process the template - substitute variables and evaluate {{ expressions }}
            let substituteTemplate (template: string) : string =
                // First substitute simple variables (outside of {{ }})
                let withVars = substituteVariables template

                // Then evaluate {{ expressions }}
                Regex.Replace(
                    withVars,
                    expressionPattern,
                    fun m ->
                        let expr = m.Groups.[1].Value.Trim()
                        evaluateExpression expr
                )

            // Substitute input values in URL parameters, headers, and body
            let substitutedUrlParams =
                baseRequest.UrlParameters
                |> List.map (fun (key, value) -> (substituteTemplate key, substituteTemplate value))

            let substitutedHeaders =
                baseRequest.Headers
                |> List.map (fun (key, value) -> (substituteTemplate key, substituteTemplate value))

            // Handle body based on dynamic body mode
            let bodyResult =
                if useDynamicJsonBody then
                    // When dynamic body mode is enabled, use the provided dynamic body
                    match dynamicBody with
                    | None -> Error(ValidationError "Dynamic body is required when UseDynamicJsonBody is enabled")
                    | Some body ->
                        match validateDynamicBody body with
                        | Error err -> Error err
                        | Ok validBody -> Ok validBody
                else
                    // Use the composed body with substitutions
                    let substitutedBody =
                        baseRequest.Body
                        |> List.map (fun (key, value) -> (substituteTemplate key, substituteTemplate value))

                    Ok substitutedBody

            match bodyResult with
            | Error err -> Error err
            | Ok finalBody ->
                // Always use JSON for request bodies (modern API standard)
                // The useDynamicJsonBody flag only controls whether body comes from user input at runtime
                let executableRequest =
                    { BaseUrl = substituteTemplate baseRequest.BaseUrl
                      UrlParameters = substitutedUrlParams
                      Headers = substitutedHeaders
                      Body = finalBody
                      HttpMethod = baseRequest.HttpMethod
                      UseJsonBody = true }

                Ok(setExecutableRequest executableRequest run)

    let markAsRunning (actorUserId: UserId) (run: ValidatedRun) : ValidatedRun =
        let updatedRunData =
            { run.State with
                Status = Running
                StartedAt = Some DateTime.UtcNow }

        let statusChangedEvent =
            RunEvents.runStatusChanged actorUserId run.State.Id run.State.Status Running

        { State = updatedRunData
          UncommittedEvents = run.UncommittedEvents @ [ statusChangedEvent :> IDomainEvent ] }

    let markAsSuccess (actorUserId: UserId) (response: string) (run: ValidatedRun) : ValidatedRun =
        let updatedRunData =
            { run.State with
                Status = Success
                Response = Some response
                CompletedAt = Some DateTime.UtcNow }

        let statusChangedEvent =
            RunEvents.runStatusChanged actorUserId run.State.Id run.State.Status Success

        { State = updatedRunData
          UncommittedEvents = run.UncommittedEvents @ [ statusChangedEvent :> IDomainEvent ] }

    let markAsFailure (actorUserId: UserId) (errorMessage: string) (run: ValidatedRun) : ValidatedRun =
        let updatedRunData =
            { run.State with
                Status = Failure
                ErrorMessage = Some errorMessage
                CompletedAt = Some DateTime.UtcNow }

        let statusChangedEvent =
            RunEvents.runStatusChanged actorUserId run.State.Id run.State.Status Failure

        { State = updatedRunData
          UncommittedEvents = run.UncommittedEvents @ [ statusChangedEvent :> IDomainEvent ] }

    let markAsInvalidConfiguration (actorUserId: UserId) (errorMessage: string) (run: ValidatedRun) : ValidatedRun =
        let updatedRunData =
            { run.State with
                Status = InvalidConfiguration
                ErrorMessage = Some errorMessage
                CompletedAt = Some DateTime.UtcNow }

        let statusChangedEvent =
            RunEvents.runStatusChanged actorUserId run.State.Id run.State.Status InvalidConfiguration

        { State = updatedRunData
          UncommittedEvents = run.UncommittedEvents @ [ statusChangedEvent :> IDomainEvent ] }

    let getUncommittedEvents (run: ValidatedRun) : IDomainEvent list = run.UncommittedEvents

    let markEventsAsCommitted (run: ValidatedRun) : ValidatedRun = { run with UncommittedEvents = [] }

    let getId (run: Run) : RunId = run.State.Id

    let getAppId (run: Run) : AppId = run.State.AppId

    let getStatus (run: Run) : RunStatus = run.State.Status

    let getInputValues (run: Run) : RunInputValue list = run.State.InputValues

    let getExecutableRequest (run: Run) : ExecutableHttpRequest option = run.State.ExecutableRequest

    let getResponse (run: Run) : string option = run.State.Response

    let getErrorMessage (run: Run) : string option = run.State.ErrorMessage

    let getStartedAt (run: Run) : DateTime option = run.State.StartedAt

    let getCompletedAt (run: Run) : DateTime option = run.State.CompletedAt

    let getCreatedAt (run: Run) : DateTime = run.State.CreatedAt
