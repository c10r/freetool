---
name: freetool-openfga-hexagonal-architecture
description: Explain and apply Freetool's OpenFGA integration using onion/hexagonal architecture boundaries, including exactly where authorization logic belongs and where it must not be implemented. Use when reviewing auth design, adding permissions, changing OpenFGA tuple writes/checks, or teaching team conventions with real code samples.
---

# Freetool OpenFGA Hexagonal Architecture

## Overview
Use this skill to produce repo-accurate guidance for OpenFGA in Freetool: where it is implemented, where it is not, and how to extend it without violating architecture boundaries.

## Workflow
1. Read boundary-defining files first.
2. Map each OpenFGA concern to API/Application/Infrastructure/Domain.
3. Explain both positive placement ("implement here") and negative placement ("do not implement here").
4. Include short F# snippets from the codebase that demonstrate best practice.
5. Call out practical caveats and extension checklist.

## Read First
- `src/Freetool.Application/src/Interfaces/IAuthorizationService.fs`
- `src/Freetool.Infrastructure/src/Services/OpenFgaService.fs`
- `src/Freetool.Api/src/Program.fs`
- `src/Freetool.Api/src/Controllers/AuthenticatedControllerBase.fs`
- `src/Freetool.Api/src/Controllers/SpaceController.fs`
- `src/Freetool.Application/src/Handlers/SpaceHandler.fs`
- `src/Freetool.Api/src/Services/IdentityProvisioningService.fs`

## Layer Mapping (Best Practice)
- `API layer (inbound adapters)`: Middleware authenticates and sets `UserId`; controllers enforce endpoint-level authorization checks and orchestration.
- `Application layer (ports + use cases)`: Defines authorization port and typed auth language (`IAuthorizationService`, `AuthSubject`, `AuthRelation`, `AuthObject`); handlers orchestrate business use cases that need permission reads/writes.
- `Infrastructure layer (outbound adapter)`: Implements OpenFGA SDK calls in `OpenFgaService`.
- `Domain layer`: Contains zero OpenFGA/SDK knowledge; no external auth client details.

## Implement Here vs Do Not Implement Here

### Implement Here
- Define authorization operations in `IAuthorizationService` (`src/Freetool.Application/src/Interfaces/IAuthorizationService.fs`).
- Implement all OpenFGA SDK interactions in `OpenFgaService` (`src/Freetool.Infrastructure/src/Services/OpenFgaService.fs`).
- Register the adapter in DI and initialize store/model in `Program.fs`.
- Perform endpoint authorization checks in controllers (for request-scoped decisions).
- Use application handlers for permission-diff orchestration and event coupling (for use-case-level permission changes).

### Do Not Implement Here
- Do not call OpenFGA SDK directly from controllers, handlers, or domain.
- Do not put OpenFGA tuple-string literals throughout API/domain logic; use typed auth unions and helpers.
- Do not place authorization business policy inside domain entities.
- Do not make Domain reference `IAuthorizationService` or infrastructure types.

## Code Samples

### 1) Application Port with Typed Auth Language
`src/Freetool.Application/src/Interfaces/IAuthorizationService.fs`
```fsharp
type IAuthorizationService =
    abstract member CreateRelationshipsAsync: RelationshipTuple list -> Task<unit>
    abstract member CheckPermissionAsync:
        subject: AuthSubject -> relation: AuthRelation -> object: AuthObject -> Task<bool>
```

### 2) Infrastructure Adapter Owns OpenFGA SDK
`src/Freetool.Infrastructure/src/Services/OpenFgaService.fs`
```fsharp
open OpenFga.Sdk.Client

type OpenFgaService(apiUrl: string, logger: ILogger<OpenFgaService>, ?storeId: string) =
    interface IAuthorizationService with
        member _.CheckPermissionAsync(subject, relation, object) : Task<bool> =
            task {
                use client = createClient ()
                let body =
                    ClientCheckRequest(
                        User = AuthTypes.subjectToString subject,
                        Relation = AuthTypes.relationToString relation,
                        Object = AuthTypes.objectToString object
                    )
                let! response = client.Check(body)
                return response.Allowed.GetValueOrDefault(false)
            }
```

### 3) DI Wiring Keeps API Depending on Port
`src/Freetool.Api/src/Program.fs`
```fsharp
builder.Services.AddScoped<IAuthorizationService>(fun serviceProvider ->
    let loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>()
    let logger = loggerFactory.CreateLogger<OpenFgaService>()
    OpenFgaService(openFgaApiUrl, logger, actualStoreId) :> IAuthorizationService)
|> ignore
```

### 4) Controller Performs Endpoint Permission Check via Port
`src/Freetool.Api/src/Controllers/AppController.fs`
```fsharp
let! hasPermission =
    authorizationService.CheckPermissionAsync
        (User(userId.Value.ToString()))
        permission
        (SpaceObject(spaceId.Value.ToString()))
```

### 5) Handler Performs Permission Diff + Atomic Tuple Update
`src/Freetool.Application/src/Handlers/SpaceHandler.fs`
```fsharp
if not (List.isEmpty tuplesToAdd) || not (List.isEmpty tuplesToRemove) then
    do!
        authService.UpdateRelationshipsAsync
            { TuplesToAdd = tuplesToAdd
              TuplesToRemove = tuplesToRemove }
```

## Runtime Flow (Request to Decision)
1. Middleware authenticates user and sets `HttpContext.Items["UserId"]`.
2. `AuthenticatedControllerBase` reads `CurrentUserId`.
3. Controller calls `IAuthorizationService.CheckPermissionAsync`.
4. `OpenFgaService` converts typed values to OpenFGA tuple strings and calls SDK.
5. Controller/handler continues or returns `403`.

## Extension Checklist
When adding new authorization behavior:
1. Add/adjust `AuthRelation` or typed auth models in `IAuthorizationService.fs`.
2. Update relation-to-string mapping in `AuthTypes`.
3. Update OpenFGA model definition in `OpenFgaService.WriteAuthorizationModelAsync`.
4. Add use-case checks in controller and/or handler via `IAuthorizationService`.
5. Add integration tests for allowed/denied paths and tuple-write behavior.

## Anti-Patterns to Flag in Review
- `open OpenFga.Sdk.*` outside infrastructure.
- Raw relation strings like `"create_app"` scattered in controllers/handlers.
- Domain entities making auth service calls.
- Controller bypassing typed `AuthSubject/AuthRelation/AuthObject` API.

## Fast Verification Commands
```bash
rg -n "open OpenFga\\.Sdk|OpenFgaService|IAuthorizationService|CheckPermissionAsync|CreateRelationshipsAsync|UpdateRelationshipsAsync" src --glob '*.fs'
```

```bash
rg -n "CurrentUserId|HttpContext\\.Items\\[\\\"UserId\\\"\\]|UseMiddleware<IapAuthMiddleware>|UseMiddleware<DevAuthMiddleware>" src/Freetool.Api/src --glob '*.fs'
```
