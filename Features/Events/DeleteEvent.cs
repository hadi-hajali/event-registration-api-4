using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Events;

public sealed record DeleteEventCommand(ulong Id) : IRequest;

public sealed class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand>
{
    private readonly IEventRegistrationDatabase _database;

    public DeleteEventCommandHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        await using var connection = await _database.CreateConnectionAsync();

        const string eventSql = """
            SELECT Id
            FROM Events
            WHERE Id = @Id
            LIMIT 1;
            """;

        var eventId = await connection.ExecuteScalarAsync<ulong?>(eventSql, new { request.Id });
        if (!eventId.HasValue)
        {
            throw new NotFoundException("Event not found.");
        }

        const string registrationSql = """
            SELECT Id
            FROM Registrations
            WHERE EventId = @Id
            LIMIT 1;
            """;

        var registrationId = await connection.ExecuteScalarAsync<ulong?>(registrationSql, new { request.Id });
        if (registrationId.HasValue)
        {
            throw new ConflictException("An event with any registration record cannot be hard deleted.");
        }

        const string deleteSql = """
            DELETE FROM Events
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(deleteSql, new { request.Id });
    }
}
