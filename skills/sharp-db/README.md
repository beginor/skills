# Sharp-DB

A CLI tool and Claude Code Skill for querying databases (PostgreSQL, MySQL, SQLite) and inspecting schema metadata.

## Features

- **Execute SQL queries** and get results as markdown tables (capped by `--limit` to avoid huge outputs)
- **List tables and views** with primary keys, foreign keys, and descriptions
- **Inspect table columns** with data types, character maximum length (for char/varchar), constraints, and foreign key references
- **Run SQL files** in a transaction, non-interactively with `--yes` (multi-statement files run as a batch)
- **Connection from environment**: pass credentials via `--connection-env <VAR>` to keep them out of the command line
- **Multi-database support**: PostgreSQL, MySQL, SQLite
- **Schema-aware**: Optional schema filtering for databases that support schemas

## Installation

### Via skills CLI (recommended)

```bash
npx skills add beginor/agent-skills --skill sharp-db
```

### From source

Prerequisites: .NET 10 SDK.

```bash
git clone https://github.com/beginor/agent-skills.git
cd agent-skills/skills/sharp-db/
./scripts/build.sh
```

The binary is at `bin/sharp-db` (or `bin/sharp-db.exe` on Windows).

## Usage

### Query — Execute SQL

```bash
sharp-db query --db-type postgres --connection "host=localhost;port=5432;database=mydb;username=postgres;password=pass" --sql "SELECT id, name FROM users"
```

`--limit N` caps the returned rows (default `100`); a capped result appends a truncation notice. Use `--limit 0` to disable the cap. The connection string can also come from an environment variable via `--connection-env <VAR>` (mutually exclusive with `--connection`).

### Tables — List tables and views

```bash
sharp-db tables --db-type postgres --connection "host=localhost;port=5432;database=mydb;username=postgres;password=pass"
```

Optional `--schema` parameter to filter by schema:

```bash
sharp-db tables --db-type postgres --connection "..." --schema public
```

### Columns — Inspect table columns

```bash
sharp-db columns --db-type postgres --connection "..." --table users
```

Optional `--schema` parameter:

```bash
sharp-db columns --db-type postgres --connection "..." --table users --schema public
```

### Execute — Run a SQL file

Execute a SQL file within a transaction. Rolls back on error.

```bash
sharp-db execute --db-type postgres --connection "..." --file migrate.sql [--yes]
```

Without `--yes`, the tool prompts `Execute? [y/N]` and requires an interactive terminal (redirected stdin is rejected). Pass `--yes` to run non-interactively (e.g. in scripts or CI). Multi-statement files run as a single batch on all three databases.

## As a Claude Code Skill

This project includes a Skill at `.claude/skills/sharp-db/SKILL.md`. When working in Claude Code, you can use `/sharp-db` to invoke database queries directly.

### Example usage in Claude Code

```
/sharp-db query --db-type sqlite --connection "Data Source=test.db" --sql "SELECT * FROM users"
```

## Supported databases

| Database | Driver | Connection string example |
|----------|--------|---------------------------|
| PostgreSQL | Npgsql | `host=localhost;port=5432;database=mydb;username=postgres;password=pass` |
| MySQL | MySql.Data | `server=localhost;port=3306;database=mydb;user=root;password=pass` |
| SQLite | Microsoft.Data.Sqlite | `Data Source=/path/to/db.sqlite` |

## Development

### Build

```bash
dotnet build
```

### Test

```bash
dotnet test
```

### Project structure

```
scripts/src/SharpDb/
├── Metadata/
│   ├── IMetadataProvider.cs         # Provider interface
│   ├── BaseMetadataProvider.cs      # Shared execution logic
│   ├── PostgresMetadataProvider.cs  # PostgreSQL SQL
│   ├── MySQLMetadataProvider.cs     # MySQL SQL
│   ├── SqliteMetadataProvider.cs    # SQLite SQL
│   └── MetadataProviderFactory.cs   # Factory by dbType
├── DatabaseOptions.cs               # Connection parameters
├── DbConnectionFactory.cs           # Connection creation
├── MarkdownTableFormatter.cs        # Result formatting
├── MetadataQueryService.cs          # Metadata queries
├── Program.cs                       # CLI entry point
└── QueryExecutor.cs                 # SQL execution

scripts/test/SharpDbTest/
└── QueryExecutorTests.cs            # Tests (SQLite in-memory)
```

## License

MIT
