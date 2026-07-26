using System.Data.Common;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using Npgsql;

namespace Beginor.SharpDb;

public interface IDbConnectionFactory {

    DbConnection CreateConnection(DatabaseOptions options);

}

public sealed class DbConnectionFactory : IDbConnectionFactory {

    public DbConnection CreateConnection(DatabaseOptions options) {
        return options.Type switch {
            "postgres" or "postgresql" => new NpgsqlConnection(options.ConnectionString),
            "mysql" => new MySqlConnection(options.ConnectionString),
            "sqlite" => new SqliteConnection(options.ConnectionString),
            _ => throw new NotSupportedException(
                $"Unsupported dbType '{options.Type}'. Supported values are postgres, mysql, sqlite."
            )
        };
    }

}
