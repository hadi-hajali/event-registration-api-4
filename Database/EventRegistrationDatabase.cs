using System.Data;
using EventRegistration.Api.Interfaces;
using MySqlConnector;
using MySqlConnector.Core;

namespace EventRegistration.Api.Database;

public sealed class EventRegistrationDatabase : IEventRegistrationDatabase
{
    private readonly string _connectionString;

    public EventRegistrationDatabase(IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("DB_CONNECTION_STRING environment variable is not set.");
        }

        var builder = new MySqlConnectionStringBuilder(connectionString.Trim());
        if (!connectionString.Contains("sslmode", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("ssl mode", StringComparison.OrdinalIgnoreCase))
        {
            builder.SslMode = MySqlSslMode.None;
        }

        _connectionString = builder.ConnectionString;
    }

    public async Task<MySqlConnection> CreateConnectionAsync()
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
