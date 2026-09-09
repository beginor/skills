using Microsoft.Data.SqlClient;

using Beginor.SharpDb;

namespace Beginor.SharpDbTest;

public sealed class SqlServerMetadataTests {

    private const string ConnectionStringEnvironmentVariable = "SHARP_DB_SQLSERVER_CONNECTION_STRING";

    [Test]
    public async Task QueryTablesAsync_ReturnsCompositeForeignKeysWithCorrectColumnMapping() {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var metadata = new MetadataQueryService(new DbConnectionFactory());

        var markdown = await metadata.QueryTablesAsync(
            "sqlserver",
            database.ConnectionString,
            database.SchemaName
        );

        Assert.That(markdown, Does.Contain(
            $"| {database.SchemaName} | {database.ChildTableName} | BASE TABLE | child metadata | child_id | child_region -> {database.SchemaName}.{database.ParentTableName}(region_id); child_code -> {database.SchemaName}.{database.ParentTableName}(code) | {database.SchemaName}.{database.ParentTableName} |"
        ));
        Assert.That(markdown, Does.Contain(
            $"| {database.SchemaName} | {database.ParentTableName} | BASE TABLE | parent metadata | region_id, code | NULL | NULL |"
        ));
    }

    [Test]
    public async Task QueryColumnsAsync_ReturnsCompositeForeignKeyReferencesWithCorrectColumnMapping() {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var metadata = new MetadataQueryService(new DbConnectionFactory());

        var markdown = await metadata.QueryColumnsAsync(
            "sqlserver",
            database.ConnectionString,
            database.ChildTableName,
            database.SchemaName
        );

        Assert.That(markdown, Is.EqualTo(
            $"""
            | table_schema | table_name | column_name | ordinal_position | data_type | character_maximum_length | is_nullable | column_default | column_description | is_primary_key | is_foreign_key | referenced_table_schema | referenced_table_name | referenced_column_name |
            | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
            | {database.SchemaName} | {database.ChildTableName} | child_id | 1 | int | NULL | NO | NULL | child identifier | YES | NO | NULL | NULL | NULL |
            | {database.SchemaName} | {database.ChildTableName} | child_region | 2 | int | NULL | NO | NULL | NULL | NO | YES | {database.SchemaName} | {database.ParentTableName} | region_id |
            | {database.SchemaName} | {database.ChildTableName} | child_code | 3 | varchar | 32 | NO | NULL | NULL | NO | YES | {database.SchemaName} | {database.ParentTableName} | code |
            | {database.SchemaName} | {database.ChildTableName} | note | 4 | text | NULL | YES | NULL | child note | NO | NO | NULL | NULL | NULL |
            """
        ));
    }

    private sealed class SqlServerTestDatabase : IAsyncDisposable {

        private SqlServerTestDatabase(
            string connectionString,
            string schemaName,
            string parentTableName,
            string childTableName
        ) {
            ConnectionString = connectionString;
            SchemaName = schemaName;
            ParentTableName = parentTableName;
            ChildTableName = childTableName;
        }

        public string ConnectionString { get; }

        public string SchemaName { get; }

        public string ParentTableName { get; }

        public string ChildTableName { get; }

        public static async Task<SqlServerTestDatabase> CreateAsync() {
            var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(connectionString)) {
                Assert.Ignore(
                    $"Set {ConnectionStringEnvironmentVariable} to run SQL Server integration tests."
                );
            }

            var suffix = Guid.NewGuid().ToString("N")[..12];
            var schemaName = $"sharp_db_{suffix}";
            var parentTableName = "parent";
            var childTableName = "child";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // A new schema cannot be referenced within the same batch, so create it separately.
            await ExecuteAsync(connection, $"create schema {schemaName};");
            await ExecuteAsync(
                connection,
                $"""
                create table {schemaName}.{parentTableName} (
                    region_id int not null,
                    code varchar(32) not null,
                    name varchar(64) not null,
                    constraint pk_parent primary key (region_id, code)
                );
                create table {schemaName}.{childTableName} (
                    child_id int not null,
                    child_region int not null,
                    child_code varchar(32) not null,
                    note text null,
                    constraint pk_child primary key (child_id),
                    constraint fk_child_parent
                        foreign key (child_region, child_code)
                        references {schemaName}.{parentTableName} (region_id, code)
                );
                exec sp_addextendedproperty 'MS_Description', N'parent metadata', 'schema', {schemaName}, 'table', {parentTableName};
                exec sp_addextendedproperty 'MS_Description', N'child metadata', 'schema', {schemaName}, 'table', {childTableName};
                exec sp_addextendedproperty 'MS_Description', N'child identifier', 'schema', {schemaName}, 'table', {childTableName}, 'column', 'child_id';
                exec sp_addextendedproperty 'MS_Description', N'child note', 'schema', {schemaName}, 'table', {childTableName}, 'column', 'note';
                """
            );

            return new SqlServerTestDatabase(
                connectionString,
                schemaName,
                parentTableName,
                childTableName
            );
        }

        public async ValueTask DisposeAsync() {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            await ExecuteAsync(
                connection,
                $"drop table if exists {SchemaName}.{ChildTableName}; drop table if exists {SchemaName}.{ParentTableName}; drop schema if exists {SchemaName};"
            );
        }

        private static async Task ExecuteAsync(SqlConnection connection, string sql) {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

    }

}
