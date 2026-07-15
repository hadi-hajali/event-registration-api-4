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

        const string existsSql = """
            SELECT COUNT(*)
            FROM Events
            WHERE Id = @Id;
            """;

        var exists = await connection.ExecuteScalarAsync<int>(existsSql, new { request.Id });
        if (exists == 0)
        {
            throw new NotFoundException("Event not found.");
        }

        const string deactivateSql = """
            UPDATE Events
            SET IsActive = 0,
                UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(deactivateSql, new { request.Id });
    }
}
