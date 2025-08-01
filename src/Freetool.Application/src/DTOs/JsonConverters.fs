namespace Freetool.Application.DTOs

open System
open System.Text.Json
open System.Text.Json.Serialization

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