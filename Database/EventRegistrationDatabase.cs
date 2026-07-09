using System.Data;
using EventRegistration.Api.Interfaces;
using MySqlConnector;

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

        _connectionString = connectionString.Trim();
    }

    public async Task<MySqlConnection> CreateConnectionAsync()
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
