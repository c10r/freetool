---
name: freetool-iap-auth-architecture
description: Explain how Google IAP authentication and OpenFGA authorization are implemented in the Freetool codebase, including where each piece belongs in onion/hexagonal architecture and how request flow works end-to-end. Use when asked to review middleware placement, auth layering, identity provisioning, OpenFGA interactions, or to provide code-sample-backed architecture explanations.
---

# Freetool IAP Auth Architecture

## Overview
Analyze and explain the authentication/authorization implementation in Freetool with precise file references and short F# code samples.

## Workflow
1. Identify the inbound authentication adapter and pipeline registration.
2. Map each auth component to onion/hexagonal layers.
3. Trace runtime request flow from HTTP entrypoint to permission checks.
4. Show how OpenFGA is abstracted behind application ports and implemented in infrastructure.
5. Return concrete snippets and architecture conclusions.

## Step 1: Locate Entry Points
Read these files first:
- `src/Freetool.Api/src/Program.fs`
- `src/Freetool.Api/src/Middleware/IapAuthMiddleware.fs`
- `src/Freetool.Api/src/Controllers/AuthenticatedControllerBase.fs`

Confirm:
- Middleware registration order.
- Dev-vs-prod auth path (`DevAuthMiddleware` vs `IapAuthMiddleware`).
- How authenticated user context is propagated (`HttpContext.Items["UserId"]`).

## Step 2: Map to Layers
Use this mapping model in the explanation:
- API layer (inbound adapters): middleware + controllers + API-facing services.
- Application layer (ports/use cases): interfaces such as `IAuthorizationService`, handlers.
- Infrastructure layer (outbound adapters): OpenFGA implementation (`OpenFgaService`) and repositories.
- Domain layer: typed IDs, entities, and business rules used by higher layers.

State placement explicitly with file paths.

## Step 3: Trace Auth Flow
Describe the request path in order:
1. HTTP request enters middleware.
2. IAP headers/JWT are validated (if enabled).
3. Identity is provisioned/updated.
4. `UserId` is attached to request context.
5. Controllers read `CurrentUserId`.
6. Controllers call `IAuthorizationService.CheckPermissionAsync` for space/org relations.

Include config keys that drive behavior:
- `Auth:IAP:ValidateJwt`
- `Auth:IAP:JwtAudience`
- `Auth:IAP:EmailHeader`
- `OpenFGA:OrgAdminEmail`

## Step 4: Explain OpenFGA Integration
Inspect:
- `src/Freetool.Application/src/Interfaces/IAuthorizationService.fs`
- `src/Freetool.Infrastructure/src/Services/OpenFgaService.fs`
- `src/Freetool.Api/src/Program.fs`

Explain:
- Port/adapter boundary (`IAuthorizationService` vs `OpenFgaService`).
- Startup model/store initialization.
- Tuple writes and permission checks.
- How org admin inheritance is modeled.

## Step 5: Provide Code Samples
Include short snippets for:
- Middleware registration in `Program.fs`.
- `IapAuthMiddleware` setting `UserId`.
- `AuthenticatedControllerBase` reading `UserId`.
- Controller-level permission check call.
- `OpenFgaService` permission check implementation.

Keep samples small and directly relevant.

## Output Requirements
- Lead with direct answer: where IAP middleware belongs architecturally.
- Include a compact “Layer Mapping” section.
- Include a compact “Request Flow” section.
- Include file references for every major claim.
- Include F# code samples, not pseudocode.
- Call out architectural caveats (for example, if orchestration is in API that could move to Application).

## Fast File Search Commands
Use fast repo search when needed:

```bash
rg -n "IapAuthMiddleware|UseMiddleware|IAuthorizationService|OpenFgaService|CurrentUserId" src/Freetool.Api src/Freetool.Application src/Freetool.Infrastructure --glob '*.fs'
```

```bash
rg -n "Auth:IAP|OpenFGA:OrgAdminEmail|JwtAudience" src/Freetool.Api --glob '*.fs' --glob '*.json'
```
