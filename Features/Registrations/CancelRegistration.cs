using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Registrations;

public sealed record CancelRegistrationCommand(ulong EventId, ulong Id) : IRequest<RegistrationResponse>;

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
            SELECT r.Id, r.Status, e.StartAt
            FROM Registrations r
            INNER JOIN Events e ON r.EventId = e.Id
            WHERE r.Id = @Id AND r.EventId = @EventId
            LIMIT 1";

        var existing = await connection.QuerySingleOrDefaultAsync<RegistrationCancellationState>(
            existingSql,
            new { request.Id, request.EventId });

        if (existing is null)
            throw new NotFoundException("Registration not found.");

        if (existing.StartAt <= DateTime.UtcNow)
            throw new BusinessException("Registration cannot be cancelled after the event starts.");

        if (existing.Status != 2)
        {
            const string updateSql = @"
                UPDATE Registrations
                SET Status = 2,
                    CancelledAt = UTC_TIMESTAMP()
                WHERE Id = @Id AND EventId = @EventId";

            await connection.ExecuteAsync(updateSql, new { request.Id, request.EventId });
        }

        const string selectSql = @"
            SELECT
                r.Id,
                r.EventId,
                e.Name AS EventName,
                r.ParticipantId,
                p.FullName AS ParticipantName,
                p.Email AS ParticipantEmail,
                p.Phone AS ParticipantPhone,
                r.Status,
                CASE r.Status WHEN 1 THEN 'Active' WHEN 2 THEN 'Cancelled' ELSE 'Unknown' END AS StatusName,
                r.Notes,
                r.RegisteredAt,
                r.CancelledAt
            FROM Registrations r
            INNER JOIN Events e ON r.EventId = e.Id
            INNER JOIN Participants p ON r.ParticipantId = p.Id
            WHERE r.Id = @Id AND r.EventId = @EventId";

        var updated = await connection.QuerySingleOrDefaultAsync<RegistrationResponse>(selectSql, new { request.Id, request.EventId });
        if (updated is null)
            throw new NotFoundException("Registration not found after cancelling.");

        return updated;
    }

    private sealed record RegistrationCancellationState(ulong Id, int Status, DateTime StartAt);
}
