using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Registrations;

public sealed record DeleteRegistrationCommand(ulong Id) : IRequest<Unit>;

public sealed class DeleteRegistrationCommandHandler : IRequestHandler<DeleteRegistrationCommand, Unit>
{
    private readonly IEventRegistrationDatabase _database;

    public DeleteRegistrationCommandHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<Unit> Handle(DeleteRegistrationCommand request, CancellationToken cancellationToken)
    {
        await using var connection = await _database.CreateConnectionAsync();

        const string existingSql = @"
            SELECT Id
            FROM Registrations
            WHERE Id = @Id
            LIMIT 1";

        var existingId = await connection.ExecuteScalarAsync<ulong?>(existingSql, new { Id = request.Id });
        if (!existingId.HasValue)
            throw new NotFoundException("Registration not found.");

        const string deleteSql = @"
            UPDATE Registrations
            SET Status = 2,
                CancelledAt = COALESCE(CancelledAt, UTC_TIMESTAMP())
            WHERE Id = @Id";

        await connection.ExecuteAsync(deleteSql, new { Id = request.Id });
        return Unit.Value;
    }
}
