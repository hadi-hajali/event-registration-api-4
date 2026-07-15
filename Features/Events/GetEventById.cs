using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Events;

public sealed record GetEventByIdQuery(ulong Id) : IRequest<EventResponse>;

public sealed class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public GetEventByIdQueryHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<EventResponse> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        await using var connection = await _database.CreateConnectionAsync();

        const string sql = """
            SELECT
                e.Id,
                e.Name,
                e.Description,
                e.Location,
                e.CategoryId,
                c.Name AS CategoryName,
                e.Capacity,
                e.StartAt,
                e.EndAt,
                e.RegistrationDeadline,
                e.IsActive,
                e.CreatedAt,
                e.UpdatedAt,
                COUNT(r.Id) AS RegisteredCount
            FROM Events e
            INNER JOIN Categories c ON c.Id = e.CategoryId
            LEFT JOIN Registrations r ON r.EventId = e.Id AND r.Status = 1
            WHERE e.Id = @Id
            GROUP BY
                e.Id,
                e.Name,
                e.Description,
                e.Location,
                e.CategoryId,
                c.Name,
                e.Capacity,
                e.StartAt,
                e.EndAt,
                e.RegistrationDeadline,
                e.IsActive,
                e.CreatedAt,
                e.UpdatedAt;
            """;

        var eventDetails = await connection.QuerySingleOrDefaultAsync<EventResponse>(sql, new { request.Id });
        if (eventDetails is null)
        {
            throw new NotFoundException("Event not found.");
        }

        return eventDetails;
    }
}
