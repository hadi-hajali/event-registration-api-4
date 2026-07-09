using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Registrations;

public sealed record GetRegistrationByIdQuery(ulong Id) : IRequest<RegistrationResponse>;

public sealed class GetRegistrationByIdQueryHandler : IRequestHandler<GetRegistrationByIdQuery, RegistrationResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public GetRegistrationByIdQueryHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<RegistrationResponse> Handle(GetRegistrationByIdQuery request, CancellationToken cancellationToken)
    {
        await using var connection = await _database.CreateConnectionAsync();

        const string sql = @"
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

        var registration = await connection.QuerySingleOrDefaultAsync<RegistrationResponse>(sql, new { Id = request.Id });
        if (registration is null)
        {
            throw new NotFoundException("Registration not found.");
        }

        return registration;
    }
}
