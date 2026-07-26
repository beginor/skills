namespace Beginor.SharpDb.Metadata;

/// <summary>
/// Shares common logic for connection management, parameter binding,
/// SQL execution, and result formatting. Subclasses only need to provide SQL queries.
/// </summary>
public abstract class BaseMetadataProvider(
    IDbConnectionFactory connectionFactory,
    DatabaseOptions options
) : IMetadataProvider {

    public Task<string> QueryTablesAsync(string? schema, CancellationToken cancellationToken = default) {
        return ExecuteAsync(
            GetTablesQuery(),
            new Dictionary<string, object?> { ["schema"] = schema ?? string.Empty },
            cancellationToken
        );
    }

    public Task<string> QueryColumnsAsync(string tableName, string? schema, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(tableName)) {
            throw new ArgumentException("Table or view name must not be empty.", nameof(tableName));
        }

        return ExecuteAsync(
            GetColumnsQuery(),
            new Dictionary<string, object?> {
                ["schema"] = schema ?? string.Empty,
                ["tableName"] = tableName
            },
            cancellationToken
        );
    }

    protected abstract string GetTablesQuery();

    protected abstract string GetColumnsQuery();

    protected async Task<string> ExecuteAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken
    ) {
        await using var connection = connectionFactory.CreateConnection(options);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = options.CommandTimeoutSeconds;

        foreach (var parameter in parameters) {
            var p = command.CreateParameter();
            p.ParameterName = parameter.Key;
            p.Value = parameter.Value ?? DBNull.Value;
            command.Parameters.Add(p);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await MarkdownTableFormatter.FormatAsync(reader, cancellationToken);
    }

}
