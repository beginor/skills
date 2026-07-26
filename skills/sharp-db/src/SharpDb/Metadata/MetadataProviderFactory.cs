namespace Beginor.SharpDb.Metadata;

internal static class MetadataProviderFactory {

    public static IMetadataProvider Create(DatabaseOptions options, IDbConnectionFactory connectionFactory) {
        return options.Type switch {
            "postgres" or "postgresql" => new PostgresMetadataProvider(connectionFactory, options),
            "mysql"                    => new MySQLMetadataProvider(connectionFactory, options),
            "sqlite"                   => new SqliteMetadataProvider(connectionFactory, options),
            _                          => throw new NotSupportedException(
                $"Unsupported dbType '{options.Type}'. Supported values are postgres, mysql, sqlite.")
        };
    }

}