namespace Freetool.Api.Tracing

open System

// Attribute to define span names for commands
[<AttributeUsage(AttributeTargets.Field)>]
type SpanNameAttribute(spanName: string) =
    inherit Attribute()
    member this.SpanName = spanName

// Attribute to mark properties that should be traced
[<AttributeUsage(AttributeTargets.Property ||| AttributeTargets.Field)>]
type TraceAttribute(attributeName: string) =
    inherit Attribute()
    member this.AttributeName = attributeName

// Attribute to define operation types
[<AttributeUsage(AttributeTargets.Field)>]
type OperationTypeAttribute(operationType: string) =
    inherit Attribute()
    member this.OperationType = operationType

// Attribute to mark sensitive data that should not be traced
[<AttributeUsage(AttributeTargets.Property ||| AttributeTargets.Field)>]
type SensitiveAttribute() =
    inherit Attribute()