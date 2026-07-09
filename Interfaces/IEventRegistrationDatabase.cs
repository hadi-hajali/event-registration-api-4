using MySqlConnector;
using System.Data;

namespace EventRegistration.Api.Interfaces;

public interface IEventRegistrationDatabase
{
    Task<MySqlConnection> CreateConnectionAsync();
}
    IDbConnection Open();
}
