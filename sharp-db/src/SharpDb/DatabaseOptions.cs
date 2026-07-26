namespace Beginor.SharpDb;

public sealed record DatabaseOptions(
    string Type,
    string ConnectionString,
    int CommandTimeoutSeconds = 30
) {

    public static DatabaseOptions Create(
        string dbType,
        string connectionString,
        int commandTimeoutSeconds = 30
    ) {
        if (string.IsNullOrWhiteSpace(dbType)) {
            throw new ArgumentException("Database type must not be empty.", nameof(dbType));
        }

        if (string.IsNullOrWhiteSpace(connectionString)) {
            throw new ArgumentException("Database connection string must not be empty.", nameof(connectionString));
        }

        return new DatabaseOptions(
            dbType.Trim().ToLowerInvariant(),
            connectionString,
            commandTimeoutSeconds
        );
    }

}
