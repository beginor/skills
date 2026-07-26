using Npgsql;

using Beginor.SharpDb;

namespace Beginor.SharpDbTest;

public sealed class PostgresMetadataTests {

    private const string ConnectionStringEnvironmentVariable = "SHARP_DB_POSTGRES_CONNECTION_STRING";

    [Test]
    public async Task QueryTablesAsync_ReturnsCompositeForeignKeysWithCorrectColumnMapping() {
        await using var database = await PostgresTestDatabase.CreateAsync();
        var metadata = new MetadataQueryService(new DbConnectionFactory());

        var markdown = await metadata.QueryTablesAsync(
            "postgres",
            database.ConnectionString,
            database.SchemaName
        );

        Assert.That(markdown, Is.EqualTo(
            $"""
            | table_schema | table_name | table_type | table_description | primary_key_columns | foreign_keys | related_objects |
            | --- | --- | --- | --- | --- | --- | --- |
            | {database.SchemaName} | child_records | BASE TABLE | NULL | child_id | child_region -> {database.SchemaName}.parent_records(region_id); child_code -> {database.SchemaName}.parent_records(code) | {database.SchemaName}.parent_records |
            | {database.SchemaName} | parent_records | BASE TABLE | NULL | region_id, code | NULL | NULL |
            """
        ));
    }

    [Test]
    public async Task QueryColumnsAsync_ReturnsCompositeForeignKeyReferencesWithCorrectColumnMapping() {
        await using var database = await PostgresTestDatabase.CreateAsync();
        var metadata = new MetadataQueryService(new DbConnectionFactory());

        var markdown = await metadata.QueryColumnsAsync(
            "postgres",
            database.ConnectionString,
            "child_records",
            database.SchemaName
        );

        Assert.That(markdown, Is.EqualTo(
            $"""
            | table_schema | table_name | column_name | ordinal_position | data_type | is_nullable | column_default | column_description | is_primary_key | is_foreign_key | referenced_table_schema | referenced_table_name | referenced_column_name |
            | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
            | {database.SchemaName} | child_records | child_id | 1 | integer | NO | NULL | NULL | YES | NO | NULL | NULL | NULL |
            | {database.SchemaName} | child_records | child_region | 2 | integer | NO | NULL | NULL | NO | YES | {database.SchemaName} | parent_records | region_id |
            | {database.SchemaName} | child_records | child_code | 3 | text | NO | NULL | NULL | NO | YES | {database.SchemaName} | parent_records | code |
            | {database.SchemaName} | child_records | note | 4 | text | YES | NULL | NULL | NO | NO | NULL | NULL | NULL |
            """
        ));
    }

    private sealed class PostgresTestDatabase : IAsyncDisposable {

        private PostgresTestDatabase(string connectionString, string schemaName) {
            ConnectionString = connectionString;
            SchemaName = schemaName;
        }

        public string ConnectionString { get; }

        public string SchemaName { get; }

        public static async Task<PostgresTestDatabase> CreateAsync() {
            var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(connectionString)) {
                Assert.Ignore(
                    $"Set {ConnectionStringEnvironmentVariable} to run PostgreSQL integration tests."
                );
            }

            var schemaName = "sharp_db_test_" + Guid.NewGuid().ToString("N");

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                create schema "{schemaName}";

                create table "{schemaName}".parent_records (
                    region_id integer not null,
                    code text not null,
                    name text not null,
                    constraint parent_records_pkey primary key (region_id, code)
                );

                create table "{schemaName}".child_records (
                    child_id integer not null,
                    child_region integer not null,
                    child_code text not null,
                    note text null,
                    constraint child_records_pkey primary key (child_id),
                    constraint child_records_parent_records_fkey
                        foreign key (child_region, child_code)
                        references "{schemaName}".parent_records (region_id, code)
                );
                """;
            await command.ExecuteNonQueryAsync();

            return new PostgresTestDatabase(connectionString, schemaName);
        }

        public async ValueTask DisposeAsync() {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $"""drop schema if exists "{SchemaName}" cascade""";
            await command.ExecuteNonQueryAsync();
        }

    }

}
