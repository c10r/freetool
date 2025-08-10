namespace Freetool.Application.DTOs

open System
open System.Text.Json
open System.Text.Json.Serialization
open Freetool.Domain.ValueObjects

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