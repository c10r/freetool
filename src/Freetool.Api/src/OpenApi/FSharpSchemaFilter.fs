namespace Freetool.Api.OpenApi

open Microsoft.OpenApi.Models
open Microsoft.OpenApi.Any
open Swashbuckle.AspNetCore.SwaggerGen
open System
open System.Collections.Generic
open Freetool.Domain.Entities

type FSharpUnionSchemaFilter() =
    interface ISchemaFilter with
        member _.Apply(schema: OpenApiSchema, context: SchemaFilterContext) =
            // Handle EventType union
            if context.Type = typeof<EventType> then
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