# Repository Guidelines

## Project Structure & Module Organization
Code lives in layered F# projects under `src/`: Domain (entities/value objects), Application (DTOs plus orchestrators), Infrastructure (EF Core, OpenFGA, external gateways), and Api (controllers, middleware, tracing). Every project has a sibling `test` directory with its xUnit suite, so place new specs beside the implementation. Shared artifacts such as `openapi.spec.json`, solution files, and `docker-compose.yml` stay at the root, while the static frontend the API serves resides in `www/`.

## Build, Test, and Development Commands
- `dotnet restore Freetool.sln` – restore NuGet packages.
- `dotnet build Freetool.sln -c Release` – compile every project and surface analyzer warnings.
- `dotnet run --project src/Freetool.Api/Freetool.Api.fsproj` – launch the API locally, serving assets from `www/`.
- `dotnet test Freetool.sln` – run all suites; append `--filter FullyQualifiedName~Group` for targeted checks.
- `docker-compose up --build freetool-api` – boot the API, Aspire dashboard, and OpenFGA with OTEL settings.

## Coding Style & Naming Conventions
Follow idiomatic F#: four-space indentation, PascalCase modules/types/DU cases, camelCase functions and values. Favor immutable records, pure pipelines, and expression-bodied helpers; isolate required mutation inside infrastructure adapters. Keep configuration constants in `appsettings.*.json`, surface telemetry through the supplied OTEL helpers, and run `dotnet format Freetool.sln` before pushing if the build surfaces spacing or analyzer warnings.

## Testing Guidelines
xUnit backs every project: use `[<Fact>]` for single scenarios and `[<Theory>]` for data-driven cases. Mirror the descriptive naming already in `RunTests.fs` and keep fixtures with the code they exercise. Prioritize domain tests, then add infrastructure or API integration specs only when a change touches persistence, OpenFGA, or OTEL. Helpers should stay deterministic, and PRs must mention any manual validation done for telemetry, migrations, or DI changes.

## Commit & Pull Request Guidelines
Match the current history: imperative, single-line subjects (`Add authz checks for controllers`) with optional context in the body. PRs should summarize the change, link the driving issue, list verification commands (`dotnet test`, manual curl, etc.), and add screenshots or trace captures whenever UI or observability behavior shifts. Always note configuration, migration, or policy updates so operators can roll them out with the code.

## Security & Configuration Tips
Keep secrets out of source control; use `appsettings.Development.json`, user secrets, or environment variables. The API expects OTEL and OpenFGA settings (`OTEL_EXPORTER_OTLP_ENDPOINT`, `OPENFGA_HTTP_ADDR`) defined in `docker-compose.yml`, so reuse those key names elsewhere. Filter sensitive payloads before emitting tracing data and prefer the Docker stack so SQLite/OpenFGA defaults stay aligned.

## Landing the Plane (Session Completion)

**When ending a work session**, you MUST complete ALL steps below. Work is NOT complete until `git push` succeeds.

**MANDATORY WORKFLOW:**

1. **File issues for remaining work** - Create issues for anything that needs follow-up
2. **Run quality gates** (if code changed) - Tests, linters, builds
3. **Update issue status** - Close finished work, update in-progress items
4. **PUSH TO REMOTE** - This is MANDATORY:
   ```bash
   git pull --rebase
   bd sync
   git push
   git status  # MUST show "up to date with origin"
   ```
5. **Clean up** - Clear stashes, prune remote branches
6. **Verify** - All changes committed AND pushed
7. **Hand off** - Provide context for next session

**CRITICAL RULES:**
- Work is NOT complete until `git push` succeeds
- NEVER stop before pushing - that leaves work stranded locally
- NEVER say "ready to push when you are" - YOU must push
- If push fails, resolve and retry until it succeeds
