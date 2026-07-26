using Beginor.SharpDb.Metadata;

namespace Beginor.SharpDb;

public sealed class MetadataQueryService(
    IDbConnectionFactory connectionFactory
) {

    public Task<string> QueryTablesAsync(
        string dbType,
        string connectionString,
        string? schema = null,
        CancellationToken cancellationToken = default
    ) {
        var options = DatabaseOptions.Create(dbType, connectionString);
        var provider = MetadataProviderFactory.Create(options, connectionFactory);
        return provider.QueryTablesAsync(schema, cancellationToken);
    }

    public Task<string> QueryColumnsAsync(
        string dbType,
        string connectionString,
        string tableName,
        string? schema = null,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(tableName)) {
            throw new ArgumentException("Table or view name must not be empty.", nameof(tableName));
        }

        var options = DatabaseOptions.Create(dbType, connectionString);
        var provider = MetadataProviderFactory.Create(options, connectionFactory);
        return provider.QueryColumnsAsync(tableName, schema, cancellationToken);
    }

}