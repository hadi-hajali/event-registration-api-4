using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Registrations;

public sealed record CancelRegistrationCommand(ulong Id) : IRequest<RegistrationResponse>;

public sealed class CancelRegistrationCommandHandler : IRequestHandler<CancelRegistrationCommand, RegistrationResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public CancelRegistrationCommandHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<RegistrationResponse> Handle(CancelRegistrationCommand request, CancellationToken cancellationToken)
    {
        await using var connection = await _database.CreateConnectionAsync();

        const string existingSql = @"
            SELECT Id, Status
            FROM Registrations
            WHERE Id = @Id
            LIMIT 1";

        var existing = await connection.QuerySingleOrDefaultAsync<dynamic>(existingSql, new { Id = request.Id });
        if (existing is null)
            throw new NotFoundException("Registration not found.");

        int status = (int)existing.Status;
        if (status == 2)
            throw new ConflictException("Registration is already cancelled.");

        const string updateSql = @"
            UPDATE Registrations
            SET Status = 2,
                CancelledAt = UTC_TIMESTAMP()
            WHERE Id = @Id";

        await connection.ExecuteAsync(updateSql, new { Id = request.Id });

        const string selectSql = @"
            SELECT
                r.Id,
                r.EventId,
                e.Name AS EventName,
                r.ParticipantId,
                p.FullName AS ParticipantName,
                p.Email AS ParticipantEmail,
                r.Status,
                r.Notes,
                r.RegisteredAt,
                r.CancelledAt
            FROM Registrations r
            INNER JOIN Events e ON r.EventId = e.Id
            INNER JOIN Participants p ON r.ParticipantId = p.Id
            WHERE r.Id = @Id";

        var updated = await connection.QuerySingleOrDefaultAsync<RegistrationResponse>(selectSql, new { Id = request.Id });
        if (updated is null)
            throw new NotFoundException("Registration not found after cancelling.");

        return updated;
    }
}
