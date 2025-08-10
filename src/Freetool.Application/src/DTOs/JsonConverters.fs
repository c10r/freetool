namespace Freetool.Application.DTOs

open System
open System.Text.Json
open System.Text.Json.Serialization
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities

type StringOptionConverter() =
    inherit JsonConverter<string option>()

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) =
        if reader.TokenType = JsonTokenType.Null then
            None
        else
            let str = reader.GetString()

            match str with
            | "" -> None
            | nonEmptyString -> Some nonEmptyString

    override _.Write(writer: Utf8JsonWriter, value: string option, _options: JsonSerializerOptions) =
        match value with
        | None -> writer.WriteNullValue()
        | Some str ->
            if String.IsNullOrEmpty(str) then
                writer.WriteNullValue()
            else
                writer.WriteStringValue(str)

type FolderLocationConverter() =
    inherit JsonConverter<FolderLocation>()

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) =
        if reader.TokenType = JsonTokenType.Null then
            RootFolder
        else
            ChildFolder(reader.GetString())

    override _.Write(writer: Utf8JsonWriter, value: FolderLocation, _options: JsonSerializerOptions) =
        match value with
        | RootFolder -> writer.WriteNullValue()
        | ChildFolder parentId -> writer.WriteStringValue(parentId)

type UserIdConverter() =
    inherit JsonConverter<UserId>()

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) =
        let guidStr = reader.GetString()
        let guid = Guid.Parse(guidStr)
        UserId.FromGuid(guid)

    override _.Write(writer: Utf8JsonWriter, value: UserId, _options: JsonSerializerOptions) =
        writer.WriteStringValue(value.Value.ToString())

type HttpMethodConverter() =
    inherit JsonConverter<HttpMethod>()

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) =
        let methodStr = reader.GetString()

        match HttpMethod.Create(methodStr) with
        | Ok httpMethod -> httpMethod
        | Error _ -> failwith $"Invalid HTTP method: {methodStr}"

    override _.Write(writer: Utf8JsonWriter, value: HttpMethod, _options: JsonSerializerOptions) =
        writer.WriteStringValue(value.ToString())

type EventTypeConverter() =
    inherit JsonConverter<EventType>()

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) =
        let eventTypeStr = reader.GetString()

        match Freetool.Domain.Entities.EventTypeConverter.fromString (eventTypeStr) with
        | Some eventType -> eventType
        | None -> failwith $"Invalid event type: {eventTypeStr}"

    override _.Write(writer: Utf8JsonWriter, value: EventType, _options: JsonSerializerOptions) =
        writer.WriteStringValue(Freetool.Domain.Entities.EventTypeConverter.toString (value))

type EntityTypeConverter() =
    inherit JsonConverter<EntityType>()

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) =
        let entityTypeStr = reader.GetString()

        match Freetool.Domain.Entities.EntityTypeConverter.fromString (entityTypeStr) with
        | Some entityType -> entityType
        | None -> failwith $"Invalid entity type: {entityTypeStr}"

    override _.Write(writer: Utf8JsonWriter, value: EntityType, _options: JsonSerializerOptions) =
        writer.WriteStringValue(Freetool.Domain.Entities.EntityTypeConverter.toString (value))