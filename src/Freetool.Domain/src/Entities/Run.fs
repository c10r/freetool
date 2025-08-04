namespace Freetool.Domain.Entities

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events
open Freetool.Domain.Services

type RunData = {
    Id: RunId
    AppId: AppId
    Status: RunStatus
    InputValues: RunInputValue list
    ExecutableRequest: ExecutableHttpRequest option
    Response: string option
    ErrorMessage: string option
    StartedAt: DateTime option
    CompletedAt: DateTime option
    CreatedAt: DateTime
}

type Run = EventSourcingAggregate<RunData>

module RunAggregateHelpers =
    let getEntityId (run: Run) : RunId = run.State.Id

    let implementsIEntity (run: Run) =
        { new IEntity<RunId> with
            member _.Id = run.State.Id
        }

// Type aliases for clarity
type UnvalidatedRun = Run // From DTOs - potentially unsafe
type ValidatedRun = Run // Validated domain model and database data

module Run =
    let fromData (runData: RunData) : ValidatedRun = {
        State = runData
        UncommittedEvents = []
    }

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

        // Check that all required inputs are provided
        let missingRequiredInputs =
            appInputs
            |> List.filter (fun input -> input.Required && not (inputValuesMap.ContainsKey input.Title))
            |> List.map (fun input -> input.Title)

        if not missingRequiredInputs.IsEmpty then
            let missingList = String.concat ", " missingRequiredInputs
            Error(ValidationError $"Missing required inputs: {missingList}")
        else
            // Check that all provided inputs exist in the app schema
            let invalidInputs =
                inputValues
                |> List.filter (fun iv -> not (appInputsMap.ContainsKey iv.Title))
                |> List.map (fun iv -> iv.Title)

            if not invalidInputs.IsEmpty then
                let invalidList = String.concat ", " invalidInputs
                Error(ValidationError $"Invalid inputs not defined in app: {invalidList}")
            else
                // TODO: Add type validation based on InputType (Email, Integer, etc.)
                Ok inputValues

    let createWithValidation (app: ValidatedApp) (inputValues: RunInputValue list) : Result<ValidatedRun, DomainError> =

        let appInputs = App.getInputs app

        match validateInputValues appInputs inputValues with
        | Error err -> Error err
        | Ok validatedInputValues ->
            let runData = {
                Id = RunId.NewId()
                AppId = App.getId app
                Status = Pending
                InputValues = validatedInputValues
                ExecutableRequest = None
                Response = None
                ErrorMessage = None
                StartedAt = None
                CompletedAt = None
                CreatedAt = DateTime.UtcNow
            }

            let runCreatedEvent =
                RunEvents.runCreated runData.Id runData.AppId validatedInputValues

            Ok {
                State = runData
                UncommittedEvents = [ runCreatedEvent :> IDomainEvent ]
            }

    let setExecutableRequest (executableRequest: ExecutableHttpRequest) (run: ValidatedRun) : ValidatedRun =
        let updatedRunData = {
            run.State with
                ExecutableRequest = Some executableRequest
        }

        {
            State = updatedRunData
            UncommittedEvents = run.UncommittedEvents
        }

    let composeExecutableRequestFromAppAndResource
        (run: ValidatedRun)
        (app: ValidatedApp)
        (resource: ValidatedResource)
        : Result<ValidatedRun, DomainError> =

        // Use existing RequestComposer to create the base ExecutableHttpRequest
        match RequestComposer.composeExecutableRequest resource app with
        | Error err -> Error err
        | Ok baseRequest ->
            // Substitute input values into the request
            let inputValuesMap =
                run.State.InputValues |> List.map (fun iv -> iv.Title, iv.Value) |> Map.ofList

            let substituteTemplate (template: string) : string =
                let mutable result = template

                for kvp in inputValuesMap do
                    let placeholder = $"{{{kvp.Key}}}"
                    result <- result.Replace(placeholder, kvp.Value)

                result

            // Substitute input values in URL parameters, headers, and body
            let substitutedUrlParams =
                baseRequest.UrlParameters
                |> List.map (fun (key, value) -> (substituteTemplate key, substituteTemplate value))

            let substitutedHeaders =
                baseRequest.Headers
                |> List.map (fun (key, value) -> (substituteTemplate key, substituteTemplate value))

            let substitutedBody =
                baseRequest.Body
                |> List.map (fun (key, value) -> (substituteTemplate key, substituteTemplate value))

            let executableRequest = {
                BaseUrl = substituteTemplate baseRequest.BaseUrl
                UrlParameters = substitutedUrlParams
                Headers = substitutedHeaders
                Body = substitutedBody
                HttpMethod = baseRequest.HttpMethod
            }

            Ok(setExecutableRequest executableRequest run)

    let markAsRunning (run: ValidatedRun) : ValidatedRun =
        let updatedRunData = {
            run.State with
                Status = Running
                StartedAt = Some DateTime.UtcNow
        }

        let statusChangedEvent =
            RunEvents.runStatusChanged run.State.Id run.State.Status Running

        {
            State = updatedRunData
            UncommittedEvents = run.UncommittedEvents @ [ statusChangedEvent :> IDomainEvent ]
        }

    let markAsSuccess (response: string) (run: ValidatedRun) : ValidatedRun =
        let updatedRunData = {
            run.State with
                Status = Success
                Response = Some response
                CompletedAt = Some DateTime.UtcNow
        }

        let statusChangedEvent =
            RunEvents.runStatusChanged run.State.Id run.State.Status Success

        {
            State = updatedRunData
            UncommittedEvents = run.UncommittedEvents @ [ statusChangedEvent :> IDomainEvent ]
        }

    let markAsFailure (errorMessage: string) (run: ValidatedRun) : ValidatedRun =
        let updatedRunData = {
            run.State with
                Status = Failure
                ErrorMessage = Some errorMessage
                CompletedAt = Some DateTime.UtcNow
        }

        let statusChangedEvent =
            RunEvents.runStatusChanged run.State.Id run.State.Status Failure

        {
            State = updatedRunData
            UncommittedEvents = run.UncommittedEvents @ [ statusChangedEvent :> IDomainEvent ]
        }

    let markAsInvalidConfiguration (errorMessage: string) (run: ValidatedRun) : ValidatedRun =
        let updatedRunData = {
            run.State with
                Status = InvalidConfiguration
                ErrorMessage = Some errorMessage
                CompletedAt = Some DateTime.UtcNow
        }

        let statusChangedEvent =
            RunEvents.runStatusChanged run.State.Id run.State.Status InvalidConfiguration

        {
            State = updatedRunData
            UncommittedEvents = run.UncommittedEvents @ [ statusChangedEvent :> IDomainEvent ]
        }

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