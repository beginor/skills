---
name: sharp-db
description: Query databases (PostgreSQL, MySQL, SQLite) and inspect schema metadata. Use when the user wants to run SQL queries, list tables/views, or inspect table columns against any database. Runs non-interactively by inferring the database type and connection string from context; only ask the user when those cannot be determined.
compatibility: requires dotnet 10.0+ installed
license: MIT
---

# Sharp-DB

A CLI tool for querying databases and inspecting schema metadata. Supports PostgreSQL, MySQL, and SQLite.

## Requirements

You should have dotnet sdk 10.0.x installed. if `dotnet` does not exist on system, please install it first:

- macOS/Linux: `curl -fsSL https://dot.net/v1/dotnet-install.sh | bash`
- Windows: `irm https://dot.net/v1/dotnet-install.ps1 | iex`

## Setup

Build the tool once before first use:

```bash
scripts/build.sh
```

The binary is at `bin/sharp-db` (or `bin/sharp-db.exe` on windows).

## Commands

### query — Execute SQL

Run a SQL statement and return results as a markdown table.

```bash
sharp-db query --db-type <postgres|mysql|sqlite> --connection "<conn-string>" --sql "<sql>" [--limit <n>]
```

For non-SELECT statements (INSERT, UPDATE, DELETE), returns `Rows affected: N`.

**Row limit.** `--limit` caps the number of rows returned (default `100`). A capped result appends `_Showing first N rows; result truncated._`. Pass `--limit 0` to remove the cap (use for exports or when you genuinely need every row). Always prefer the default for exploratory queries so a large table doesn't flood the output; raise the limit deliberately when the user asks for more.

**Mutating statements.** If the user explicitly asked for the change, run it directly and show the SQL in your report — no confirmation round-trip. Only pause for explicit confirmation when a destructive statement (DROP, TRUNCATE, DELETE/UPDATE without WHERE, ALTER) is *not* clearly implied by the user's request. SELECT and other read-only statements never require confirmation.

### tables — List tables and views

List all tables and views with metadata (primary keys, foreign keys, descriptions, related objects).

```bash
sharp-db tables --db-type <postgres|mysql|sqlite> --connection "<conn-string>" [--schema <name>]
```

If `--schema` is omitted and the database supports schemas, returns tables from all schemas.

### columns — Inspect table columns

List columns for a specific table or view, including data types, **character maximum length** (for char/varchar types), constraints, nullability, and foreign key references.

```bash
sharp-db columns --db-type <postgres|mysql|sqlite> --connection "<conn-string>" --table <name> [--schema <name>]
```

### execute — Execute a SQL file

Execute a SQL file within a transaction. Rolls back on error. Multi-statement files are run as a single batch (all three providers support it natively).

```bash
sharp-db execute --db-type <postgres|mysql|sqlite> --connection "<conn-string>" --file <path-to-sql-file> [--yes]
```

Without `--yes`, the tool prompts `Execute? [y/N]` and requires an interactive terminal (redirected stdin is rejected). **When running from Claude Code or any script, always pass `--yes`** so the command can run non-interactively — interactive prompts are impossible in that context.

## Connection string

Every command accepts the connection string via **`--connection "<string>"`** or, to keep credentials out of the command line, **`--connection-env <VAR_NAME>`** (reads the value from the named environment variable). Never pass both. Prefer `--connection-env` when the connection string is already available as an environment variable (e.g. `DATABASE_URL`-style vars), since it avoids embedding a plaintext password in the process list or shell history.

| Database | Example |
|----------|---------|
| PostgreSQL | `host=localhost;port=5432;database=mydb;username=postgres;password=pass` |
| PostgreSQL | `server=127.0.0.1;port=5432;database=test_db;user id=postgres;password=pgsql@18` |
| MySQL | `server=localhost;port=3306;database=mydb;user=root;password=pass` |
| SQLite | `Data Source=/path/to/db.sqlite` |
| SQLite | `Data Source=:memory:` |

## Workflow

The goal is to run with **no user interaction**. Only ask the user when a value truly cannot be determined.

1. **Identify the database type** — Infer from context first: the user's words, files in the working directory (e.g. `.sql`/`.db`/`.sqlite` files, `docker-compose.yml`, `appsettings.json`, `DATABASE_URL` / `PGPASSWORD`-style env vars, ORM configs). Only ask if inference is ambiguous.
2. **Obtain the connection string** — Build it from the same context (env vars, config files, a `.db`/`.sqlite` file path, `localhost` defaults). If it already lives in an environment variable, pass it via `--connection-env <VAR>` instead of `--connection` to keep credentials out of the command line. Only ask the user for credentials if they are missing and required.
3. **Build the binary if needed** — If `bin/sharp-db` does not exist, run `scripts/build.sh` first (and install the .NET 10 SDK if `dotnet` is missing).
4. **Choose the command** — `query` for SQL execution, `tables` for schema listing, `columns` for column inspection, `execute` for running SQL files.
5. **Run and present** — Execute via Bash and present the markdown output to the user.
6. **Chain for exploration** — For multi-step tasks (list tables → inspect columns → run query), chain commands naturally without asking for confirmation between steps.
7. **Run `execute` non-interactively** — Always pass `--yes` with `execute` so it can run in this context (the interactive `y/N` prompt is impossible with redirected stdin). Still show the user the SQL file contents before running.

## Examples

### Run a query

```bash
sharp-db query --db-type postgres --connection "host=localhost;port=5432;database=mydb;username=postgres;password=pass" --sql "SELECT count(*) FROM users"
```

### Run a query from an environment connection string, capped at 50 rows

```bash
sharp-db query --db-type postgres --connection-env DATABASE_URL --limit 50 --sql "SELECT id, name FROM users ORDER BY id"
```

### List all tables

```bash
sharp-db tables --db-type mysql --connection "server=localhost;port=3306;database=mydb;user=root;password=pass"
```

### Filter tables by schema

```bash
sharp-db tables --db-type postgres --connection "host=localhost;port=5432;database=mydb;username=postgres;password=pass" --schema public
```

### Inspect columns of a table

```bash
sharp-db columns --db-type sqlite --connection "Data Source=test.db" --table users
```

### Execute an update

```bash
sharp-db query --db-type postgres --connection "host=localhost;port=5432;database=mydb;username=postgres;password=pass" --sql "UPDATE users SET active = true WHERE id = 1"
```

## Error handling

- If the tool returns an error, present the error message to the user and suggest checking the connection string or database type.
- If a table or column is not found, inform the user and suggest running `tables` or `columns` to discover available names.
- If the database is unreachable, suggest verifying network connectivity and credentials.

## Notes

- `query` caps results at 100 rows by default; the output ends with a truncation notice when the cap is hit. Use `--limit N` to change it or `--limit 0` to disable.
- SQLite uses in-memory databases when `Data Source=:memory:` is specified; each connection creates a new database.
- PostgreSQL and MySQL queries include table descriptions (comments) when available.
- Foreign key information includes the referenced table and column for easy relationship tracing.
- The `columns` command includes a `character_maximum_length` column for string types (`char`/`varchar`). PostgreSQL also supports `varchar[]` array element length extraction. Non-string types show `NULL`.
