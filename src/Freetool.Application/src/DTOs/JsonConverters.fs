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
        | Error _ -> raise (JsonException($"Invalid HTTP method: {methodStr}"))

    override _.Write(writer: Utf8JsonWriter, value: HttpMethod, _options: JsonSerializerOptions) =
        writer.WriteStringValue(value.ToString())

type EventTypeConverter() =
    inherit JsonConverter<EventType>()

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) =
        let eventTypeStr = reader.GetString()

        match Freetool.Domain.Entities.EventTypeConverter.fromString (eventTypeStr) with
        | Some eventType -> eventType
        | None -> raise (JsonException($"Invalid event type: {eventTypeStr}"))

    override _.Write(writer: Utf8JsonWriter, value: EventType, _options: JsonSerializerOptions) =
        writer.WriteStringValue(Freetool.Domain.Entities.EventTypeConverter.toString (value))

type EntityTypeConverter() =
    inherit JsonConverter<EntityType>()

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) =
        let entityTypeStr = reader.GetString()

        match Freetool.Domain.Entities.EntityTypeConverter.fromString (entityTypeStr) with
        | Some entityType -> entityType
        | None -> raise (JsonException($"Invalid entity type: {entityTypeStr}"))

    override _.Write(writer: Utf8JsonWriter, value: EntityType, _options: JsonSerializerOptions) =
        writer.WriteStringValue(Freetool.Domain.Entities.EntityTypeConverter.toString (value))

type KeyValuePairConverter() =
    inherit JsonConverter<KeyValuePair>()

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) =
        if reader.TokenType <> JsonTokenType.StartObject then
            raise (JsonException("Expected start of object for KeyValuePair"))

        let mutable key = ""
        let mutable value = ""

        while reader.Read() do
            match reader.TokenType with
            | JsonTokenType.PropertyName ->
                let propertyName = reader.GetString()
                reader.Read() |> ignore

                match propertyName with
                | "key" -> key <- reader.GetString()
                | "value" -> value <- reader.GetString()
                | _ -> reader.Skip()
            | JsonTokenType.EndObject -> ()
            | _ -> reader.Skip()

        match KeyValuePair.Create(key, value) with
        | Ok kvp -> kvp
        | Error err -> raise (JsonException($"Failed to create KeyValuePair: {err}"))

    override _.Write(writer: Utf8JsonWriter, value: KeyValuePair, _options: JsonSerializerOptions) =
        writer.WriteStartObject()
        writer.WriteString("key", value.Key)
        writer.WriteString("value", value.Value)
        writer.WriteEndObject()

type FolderLocationConverter() =
    inherit JsonConverter<FolderLocation>()

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) =
        if reader.TokenType = JsonTokenType.Null then
            RootFolder
        else
            let str = reader.GetString()

            if String.IsNullOrWhiteSpace(str) then
                RootFolder
            else
                ChildFolder str

    override _.Write(writer: Utf8JsonWriter, value: FolderLocation, _options: JsonSerializerOptions) =
        match value with
        | RootFolder -> writer.WriteNullValue()
        | ChildFolder parentId -> writer.WriteStringValue(parentId)
