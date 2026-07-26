namespace Beginor.SharpDb.Metadata;

/// <summary>
/// Responsible for metadata queries for a specific database type.
/// Each implementation contains the SQL query definitions for that database
/// and handles connection management, execution, and result formatting.
/// </summary>
public interface IMetadataProvider {

    /// <summary>Queries tables and views in the database, returning a markdown table.</summary>
    Task<string> QueryTablesAsync(string? schema, CancellationToken cancellationToken = default);

    /// <summary>Queries column information for a specified table, returning a markdown table.</summary>
    Task<string> QueryColumnsAsync(string tableName, string? schema, CancellationToken cancellationToken = default);

}