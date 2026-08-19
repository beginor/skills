# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test

```bash
cd scripts
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~QueryExecutorTests.ExecuteQueryAsync_ReturnsMarkdownTable"
```

Single test via filter: `dotnet test --filter "FullyQualifiedName~<TestName>"` or `dotnet test --filter "FullyQualifiedName~<ClassName>"`.

## Architecture

**Sharp-DB** is a CLI tool and Claude Code Skill for querying PostgreSQL, MySQL, and SQLite databases.

### Core modules (`scripts/src/SharpDb/`)

- **`Program.cs`** — CLI entry point with four commands: `query`, `tables`, `columns`, `execute`. Argument parsing is manual (no framework). Shared flags: `--connection` / `--connection-env <VAR>` (mutually exclusive, read from the environment), and for `execute` a valueless `--yes` to skip interactive confirmation (required in non-terminal contexts). `query` takes `--limit <n>` (default 100, `0` disables).
- **`QueryExecutor.cs`** — Executes arbitrary SQL and returns results as markdown tables, honoring a row limit with a truncation notice. Non-SELECT statements return `Rows affected: N`. `ExecuteFileAsync` runs the file as a single batch within a transaction (all providers, including SQLite, execute multi-statement batches natively).
- **`MetadataQueryService.cs`** — Delegates to the appropriate `IMetadataProvider` via `MetadataProviderFactory`.
- **`Metadata/`** — Provider pattern for schema introspection:
  - `IMetadataProvider` — interface with `QueryTablesAsync` and `QueryColumnsAsync`. Column metadata includes `character_maximum_length` for string types (char/varchar).
  - `BaseMetadataProvider` — abstract base sharing connection management, parameter binding, and result formatting. Subclasses only supply SQL queries.
  - `PostgresMetadataProvider`, `MySQLMetadataProvider`, `SqliteMetadataProvider` — concrete implementations with database-specific SQL. Postgres provider also extracts `varchar[]` array element lengths.
  - `MetadataProviderFactory` — selects provider by `dbType`.
- **`DbConnectionFactory.cs`** — Factory creating ADO.NET `DbConnection` instances (Npgsql, MySql.Data, Sqlite).
- **`MarkdownTableFormatter.cs`** — Converts `DbDataReader` to GitHub-flavored markdown tables with proper escaping.

### Test project (`scripts/test/SharpDbTest/`)

- Uses **NUnit** with SQLite in-memory databases.
- `InMemorySqliteDatabase` helper creates a shared connection with seed data (people, posts tables + active_people view) and provides `CreateExecutor()` / `CreateMetadataQueryService()` methods.
- `SharedConnectionFactory` and `NonDisposingSqliteConnection` ensure tests share a single connection without disposal interference.

### Skill integration (`.claude/skills/sharp-db/SKILL.md`)

The skill is invoked via `/sharp-db` in Claude Code. It wraps the CLI binary and handles the workflow of identifying database type, building connection strings, and chaining commands for schema exploration.

### Connection string aliases

`DbConnectionFactory` accepts both `postgres` and `postgresql` as type values.
