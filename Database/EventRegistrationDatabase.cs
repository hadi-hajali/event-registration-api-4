using System.Data;
using EventRegistration.Api.Interfaces;
using MySqlConnector;

namespace EventRegistration.Api.Database;

public sealed class EventRegistrationDatabase : IEventRegistrationDatabase
{
    private readonly IConfiguration _configuration;

    public EventRegistrationDatabase(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection Open()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string is missing.");
        }

        return new MySqlConnection(connectionString);
    }
}