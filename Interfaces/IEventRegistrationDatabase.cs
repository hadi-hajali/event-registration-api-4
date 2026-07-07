using MySqlConnector;

namespace EventRegistration.Api.Interfaces;

public interface IEventRegistrationDatabase
{
    Task<MySqlConnection> CreateConnectionAsync();
}
