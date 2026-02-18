---
name: db-migrations
description: Create, run, and troubleshoot Freetool database migrations with DBUp. Use when changing persistence schema, adding SQL migration scripts, wiring EmbeddedResource entries, setting up a database for the first time, checking applied migration history, or handling EF Core model changes that require DBUp SQL scripts.
---

# DB Migrations

## Overview
Use this skill to manage Freetool schema changes with the repository's DBUp-based workflow.
Treat DBUp scripts as the source of truth for database evolution.

## Core Rules
- Use DBUp SQL scripts in `src/Freetool.Infrastructure/src/Database/Migrations/` as the migration mechanism.
- Keep script naming pattern: `DatabaseUpgradeScripts.DBUP.{number}_{description}.sql`.
- Add every new SQL script to `src/Freetool.Infrastructure/Freetool.Infrastructure.fsproj` as an `<EmbeddedResource ... />`.
- Run migrations by starting the API. Startup calls `Persistence.upgradeDatabase` in `src/Freetool.Api/src/Program.fs`.
- Remember: DBUp does not auto-generate SQL from EF Core model changes. Write SQL directly, or use EF tooling only as an optional draft generator.

## Workflow: Add a Migration
1. Implement model changes first (entity record, DTO mapping, EF config in `FreetoolDbContext.fs` as needed).
2. Find the next migration number from existing files in `src/Freetool.Infrastructure/src/Database/Migrations/`.
3. Create a new SQL file with the next number and a clear action name.
4. Write idempotent-safe SQL when possible (`IF NOT EXISTS`, guards, backfill order, safe defaults).
5. Add the file path as `EmbeddedResource` in `src/Freetool.Infrastructure/Freetool.Infrastructure.fsproj`.
6. Build and run API to apply migration:
```bash
dotnet build Freetool.sln
dotnet run --project src/Freetool.Api/Freetool.Api.fsproj
```
7. Verify migration was applied by inspecting DBUp journal:
```bash
sqlite3 freetool.db "select ScriptName, Applied from SchemaVersions order by Applied desc limit 20;"
```
8. Run tests relevant to changed persistence behavior.

## EF Model Change Handling
Use one of these paths:

### Path A: Recommended (manual SQL)
- Translate EF model change intent into explicit SQL.
- Keep SQL aligned with runtime mapping in `src/Freetool.Infrastructure/src/Database/FreetoolDbContext.fs`.
- Validate read + write behavior, not only schema shape.

### Path B: Optional assisted draft (EF-generated SQL, then convert)
- Use EF migrations locally to draft SQL diff.
- Copy needed SQL into a DBUp `DatabaseUpgradeScripts.DBUP.*.sql` file.
- Do not rely on EF migration history table for production flow.
- Remove scratch EF migration artifacts if created.

## Run Migrations in Common Scenarios

### First-time local database
- Set `ConnectionStrings:DefaultConnection` (for local dev this is typically SQLite).
- Start API once:
```bash
dotnet run --project src/Freetool.Api/Freetool.Api.fsproj
```
- DBUp creates DB and applies all embedded scripts.

### First-time Docker database
- Start stack:
```bash
docker-compose up --build
```
- API startup applies DBUp scripts against the mounted `freetool-db` volume.

### Apply new migrations to existing database
- Pull latest code.
- Start API (local or container).
- Confirm latest script appears in `SchemaVersions`.

### Reset a disposable local database
- Stop API.
- Delete local SQLite file used by your current connection string.
- Start API again to recreate DB and re-run all migrations.

## Verification Checklist
- New SQL file exists in `src/Freetool.Infrastructure/src/Database/Migrations/`.
- `.fsproj` includes matching `<EmbeddedResource Include="src/Database/Migrations/..." />` entry.
- API startup log shows DBUp script discovery and successful upgrade.
- `SchemaVersions` contains the new script.
- End-to-end behavior works for reads/writes touching the changed schema.

## Troubleshooting
- Migration did not run: check filename pattern and `EmbeddedResource` entry.
- Column saves but not returned on GET: add/update EF conversion in `src/Freetool.Infrastructure/src/Database/FreetoolDbContext.fs`.
- Startup fails during upgrade: test SQL directly in SQLite, fix ordering/dependencies, retry on clean local DB if needed.
- Script re-ran unexpectedly: check script name changes; DBUp tracks by script name in `SchemaVersions`.
