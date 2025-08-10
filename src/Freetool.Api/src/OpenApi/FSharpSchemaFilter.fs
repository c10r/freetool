namespace Freetool.Api.OpenApi

open Microsoft.OpenApi.Models
open Microsoft.OpenApi.Any
open Swashbuckle.AspNetCore.SwaggerGen
open System
open System.Collections.Generic
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects

type FSharpUnionSchemaFilter() =
    interface ISchemaFilter with
        member _.Apply(schema: OpenApiSchema, context: SchemaFilterContext) =
            // Handle ID value objects - convert them to simple UUID strings
            if
                context.Type = typeof<UserId>
                || context.Type = typeof<AppId>
                || context.Type = typeof<GroupId>
                || context.Type = typeof<FolderId>
                || context.Type = typeof<ResourceId>
                || context.Type = typeof<RunId>
            then
                schema.Type <- "string"
                schema.Format <- "uuid"
                schema.Properties <- null
                schema.AdditionalProperties <- null
                schema.Required <- null

            // Handle string-based value objects - convert them to simple strings
            elif
                context.Type = typeof<AppName>
                || context.Type = typeof<BaseUrl>
                || context.Type = typeof<Email>
                || context.Type = typeof<FolderName>
                || context.Type = typeof<ResourceDescription>
                || context.Type = typeof<ResourceName>
                || context.Type = typeof<Url>
                || context.Type = typeof<InputTitle>
            then
                schema.Type <- "string"
                schema.Properties <- null
                schema.AdditionalProperties <- null
                schema.Required <- null

            // Handle F# option types - convert them to nullable types
            elif
                context.Type.IsGenericType
                && context.Type.GetGenericTypeDefinition().Name.Contains("FSharpOption")
            then
                let innerType = context.Type.GetGenericArguments().[0]

                if innerType = typeof<string> then
                    schema.Type <- "string"
                    schema.Nullable <- true
                    schema.Properties <- null
                    schema.AdditionalProperties <- null
                    schema.Required <- null
                // Handle string list option types - convert them to nullable string arrays
                elif
                    innerType.IsGenericType
                    && innerType.GetGenericTypeDefinition().Name.Contains("FSharpList")
                then
                    let listItemType = innerType.GetGenericArguments().[0]

                    if listItemType = typeof<string> then
                        schema.Type <- "array"
                        schema.Items <- OpenApiSchema(Type = "string")
                        schema.Nullable <- true
                        schema.Properties <- null
                        schema.AdditionalProperties <- null
                        schema.Required <- null

            // Handle EventType union
            elif context.Type = typeof<EventType> then
                schema.Type <- "object"
                schema.Properties <- Dictionary<string, OpenApiSchema>()

                schema.Properties.Add(
                    "Case",
                    OpenApiSchema(
                        Type = "string",
                        Enum = [|
                            OpenApiString("UserEvents") :> IOpenApiAny
                            OpenApiString("AppEvents") :> IOpenApiAny
                            OpenApiString("ResourceEvents") :> IOpenApiAny
                            OpenApiString("FolderEvents") :> IOpenApiAny
                            OpenApiString("GroupEvents") :> IOpenApiAny
                            OpenApiString("RunEvents") :> IOpenApiAny
                        |]
                    )
                )

                schema.Properties.Add(
                    "Fields",
                    OpenApiSchema(
                        Type = "array",
                        Items =
                            OpenApiSchema(
                                Type = "object",
                                Properties =
                                    Dictionary<string, OpenApiSchema>(dict [ "Case", OpenApiSchema(Type = "string") ])
                            )
                    )
                )

                schema.AdditionalProperties <- null
                schema.Required <- HashSet<string>([ "Case" ])

            // Handle EntityType union
            elif context.Type = typeof<EntityType> then
                schema.Type <- "object"
                schema.Properties <- Dictionary<string, OpenApiSchema>()

                schema.Properties.Add(
                    "Case",
                    OpenApiSchema(
                        Type = "string",
                        Enum = [|
                            OpenApiString("User") :> IOpenApiAny
                            OpenApiString("App") :> IOpenApiAny
                            OpenApiString("Resource") :> IOpenApiAny
                            OpenApiString("Folder") :> IOpenApiAny
                            OpenApiString("Group") :> IOpenApiAny
                            OpenApiString("Run") :> IOpenApiAny
                        |]
                    )
                )

                schema.AdditionalProperties <- null
                schema.Required <- HashSet<string>([ "Case" ])