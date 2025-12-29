namespace Freetool.Application.Services

open System
open System.Threading.Tasks
open System.Text.Json
open System.Text.Json.Serialization
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events
open Freetool.Application.Interfaces
open Freetool.Application.DTOs

type IEventEnhancementService =
    abstract member EnhanceEventAsync: EventData -> Task<EnhancedEventData>

type EventEnhancementService
    (userRepository: IUserRepository, appRepository: IAppRepository, groupRepository: IGroupRepository) =

    // Shared JSON options for deserializing F# event types
    let jsonOptions =
        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter())
        options

    let extractEntityNameFromEventDataAsync
        (eventData: string)
        (eventType: EventType)
        (entityType: EntityType)
        : Task<string> =
        task {
            try
                match eventType with
                | UserEvents userEvent ->
                    match userEvent with
                    | UserCreatedEvent ->
                        let userEventData =
                            JsonSerializer.Deserialize<Freetool.Domain.Events.UserCreatedEvent>(eventData, jsonOptions)

                        return userEventData.Name
                    | UserInvitedEvent ->
                        let userEventData =
                            JsonSerializer.Deserialize<Freetool.Domain.Events.UserInvitedEvent>(eventData, jsonOptions)

                        // Use email as entity name since invited users don't have a name yet
                        return userEventData.Email.Value
                    | UserActivatedEvent ->
                        let userEventData =
                            JsonSerializer.Deserialize<Freetool.Domain.Events.UserActivatedEvent>(
                                eventData,
                                jsonOptions
                            )

                        return userEventData.Name
                    | _ ->
                        // For update/delete events, try to get name from data or fallback
                        let jsonDocument = JsonDocument.Parse(eventData)
                        let root = jsonDocument.RootElement
                        let mutable nameElement = Unchecked.defaultof<JsonElement>

                        if root.TryGetProperty("Name", &nameElement) then
                            return nameElement.GetString()
                        else
                            return "User (Unknown)"
                | AppEvents appEvent ->
                    match appEvent with
                    | AppCreatedEvent ->
                        let appEventData =
                            JsonSerializer.Deserialize<Freetool.Domain.Events.AppCreatedEvent>(eventData, jsonOptions)

                        return appEventData.Name.Value
                    | _ ->
                        // For update/delete events, try to get name from changes or fallback
                        let jsonDocument = JsonDocument.Parse(eventData)
                        let root = jsonDocument.RootElement
                        let mutable nameElement = Unchecked.defaultof<JsonElement>

                        if root.TryGetProperty("Name", &nameElement) then
                            return nameElement.GetString()
                        else
                            return "App (Unknown)"
                | FolderEvents folderEvent ->
                    match folderEvent with
                    | FolderCreatedEvent ->
                        let folderEventData =
                            JsonSerializer.Deserialize<Freetool.Domain.Events.FolderCreatedEvent>(
                                eventData,
                                jsonOptions
                            )

                        return folderEventData.Name.Value
                    | _ ->
                        // For update/delete events, try to get name from changes or fallback
                        let jsonDocument = JsonDocument.Parse(eventData)
                        let root = jsonDocument.RootElement
                        let mutable nameElement = Unchecked.defaultof<JsonElement>

                        if root.TryGetProperty("Name", &nameElement) then
                            return nameElement.GetString()
                        else
                            return "Folder (Unknown)"
                | ResourceEvents resourceEvent ->
                    match resourceEvent with
                    | ResourceCreatedEvent ->
                        let resourceEventData =
                            JsonSerializer.Deserialize<Freetool.Domain.Events.ResourceCreatedEvent>(
                                eventData,
                                jsonOptions
                            )

                        return resourceEventData.Name.Value
                    | _ ->
                        // For update/delete events, try to get name from changes or fallback
                        let jsonDocument = JsonDocument.Parse(eventData)
                        let root = jsonDocument.RootElement
                        let mutable nameElement = Unchecked.defaultof<JsonElement>

                        if root.TryGetProperty("Name", &nameElement) then
                            return nameElement.GetString()
                        else
                            return "Resource (Unknown)"
                | GroupEvents groupEvent ->
                    match groupEvent with
                    | GroupCreatedEvent ->
                        let groupEventData =
                            JsonSerializer.Deserialize<Freetool.Domain.Events.GroupCreatedEvent>(eventData, jsonOptions)

                        return groupEventData.Name
                    | GroupUpdatedEvent ->
                        // GroupUpdatedEvent doesn't have Name directly, look up from repository
                        let groupEventData =
                            JsonSerializer.Deserialize<Freetool.Domain.Events.GroupUpdatedEvent>(eventData, jsonOptions)

                        let! group = groupRepository.GetByIdAsync groupEventData.GroupId

                        match group with
                        | Some g -> return Group.getName g
                        | None -> return $"Group {groupEventData.GroupId.Value}"
                    | GroupDeletedEvent ->
                        // GroupDeletedEvent doesn't have Name, look up from repository
                        let groupEventData =
                            JsonSerializer.Deserialize<Freetool.Domain.Events.GroupDeletedEvent>(eventData, jsonOptions)

                        let! group = groupRepository.GetByIdAsync groupEventData.GroupId

                        match group with
                        | Some g -> return Group.getName g
                        | None -> return $"Group {groupEventData.GroupId.Value}"
                | RunEvents runEvent ->
                    match runEvent with
                    | RunCreatedEvent ->
                        let runEventData =
                            JsonSerializer.Deserialize<Freetool.Domain.Events.RunCreatedEvent>(eventData, jsonOptions)

                        // Check if AppId is empty/null GUID
                        if runEventData.AppId.Value = Guid.Empty then
                            return "Run (Empty App ID)"
                        else
                            let! app = appRepository.GetByIdAsync runEventData.AppId

                            match app with
                            | Some appData -> return App.getName appData
                            | None -> return $"Run (App {runEventData.AppId.Value} not found)"
                    | RunStatusChangedEvent ->
                        // For status changes, we don't have the AppId directly, so fallback to Run ID
                        let runEventData =
                            JsonSerializer.Deserialize<Freetool.Domain.Events.RunStatusChangedEvent>(
                                eventData,
                                jsonOptions
                            )

                        return $"Run {runEventData.RunId.Value}"
                | WorkspaceEvents workspaceEvent ->
                    match workspaceEvent with
                    | WorkspaceCreatedEvent ->
                        let workspaceEventData =
                            JsonSerializer.Deserialize<Freetool.Domain.Events.WorkspaceCreatedEvent>(
                                eventData,
                                jsonOptions
                            )

                        return $"Workspace {workspaceEventData.WorkspaceId.Value}"
                    | _ ->
                        // For update/delete events, try to get workspace ID
                        let jsonDocument = JsonDocument.Parse(eventData)
                        let root = jsonDocument.RootElement
                        let mutable workspaceIdElement = Unchecked.defaultof<JsonElement>

                        if root.TryGetProperty("WorkspaceId", &workspaceIdElement) then
                            return $"Workspace {workspaceIdElement.GetString()}"
                        else
                            return "Workspace (Unknown)"
            with ex ->
                let entityTypeStr = EntityTypeConverter.toString entityType
                // Include some of the raw event data for debugging
                let dataPreview =
                    if eventData.Length > 100 then
                        eventData.Substring(0, 100) + "..."
                    else
                        eventData

                return $"{entityTypeStr} (Parse Error: {ex.Message} | Data: {dataPreview})"
        }

    let getUserNameAsync (userId: UserId) : Task<string> =
        task {
            try
                let! user = userRepository.GetByIdAsync userId

                match user with
                | Some u -> return User.getName u
                | None -> return $"User {userId.Value}"
            with _ ->
                return $"User {userId.Value}"
        }

    let generateEventSummary (eventType: EventType) (entityName: string) (userName: string) : string =
        match eventType with
        | UserEvents userEvent ->
            match userEvent with
            | UserCreatedEvent -> $"{userName} created user \"{entityName}\""
            | UserUpdatedEvent -> $"{userName} updated user \"{entityName}\""
            | UserDeletedEvent -> $"{userName} deleted user \"{entityName}\""
            | UserInvitedEvent -> $"{userName} invited user \"{entityName}\""
            | UserActivatedEvent -> $"User \"{entityName}\" activated their account"
        | AppEvents appEvent ->
            match appEvent with
            | AppCreatedEvent -> $"{userName} created app \"{entityName}\""
            | AppUpdatedEvent -> $"{userName} updated app \"{entityName}\""
            | AppDeletedEvent -> $"{userName} deleted app \"{entityName}\""
        | FolderEvents folderEvent ->
            match folderEvent with
            | FolderCreatedEvent -> $"{userName} created folder \"{entityName}\""
            | FolderUpdatedEvent -> $"{userName} updated folder \"{entityName}\""
            | FolderDeletedEvent -> $"{userName} deleted folder \"{entityName}\""
        | ResourceEvents resourceEvent ->
            match resourceEvent with
            | ResourceCreatedEvent -> $"{userName} created resource \"{entityName}\""
            | ResourceUpdatedEvent -> $"{userName} updated resource \"{entityName}\""
            | ResourceDeletedEvent -> $"{userName} deleted resource \"{entityName}\""
        | GroupEvents groupEvent ->
            match groupEvent with
            | GroupCreatedEvent -> $"{userName} created group \"{entityName}\""
            | GroupUpdatedEvent -> $"{userName} updated group \"{entityName}\""
            | GroupDeletedEvent -> $"{userName} deleted group \"{entityName}\""
        | RunEvents runEvent ->
            match runEvent with
            | RunCreatedEvent -> $"{userName} ran \"{entityName}\""
            | RunStatusChangedEvent -> $"{userName} changed status of \"{entityName}\""
        | WorkspaceEvents workspaceEvent ->
            match workspaceEvent with
            | WorkspaceCreatedEvent -> $"{userName} created workspace \"{entityName}\""
            | WorkspaceUpdatedEvent -> $"{userName} updated workspace \"{entityName}\""
            | WorkspaceDeletedEvent -> $"{userName} deleted workspace \"{entityName}\""

    interface IEventEnhancementService with
        member this.EnhanceEventAsync(event: EventData) : Task<EnhancedEventData> =
            task {
                let! userName = getUserNameAsync event.UserId

                let! entityName = extractEntityNameFromEventDataAsync event.EventData event.EventType event.EntityType

                let eventSummary = generateEventSummary event.EventType entityName userName

                return
                    { Id = event.Id
                      EventId = event.EventId
                      EventType = event.EventType
                      EntityType = event.EntityType
                      EntityId = event.EntityId
                      EntityName = entityName
                      EventData = event.EventData
                      OccurredAt = event.OccurredAt
                      CreatedAt = event.CreatedAt
                      UserId = event.UserId
                      UserName = userName
                      EventSummary = eventSummary }
            }
