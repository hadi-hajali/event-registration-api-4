using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Events;

public sealed record GetEventByIdQuery(ulong Id) : IRequest<EventDetailsResponse>;

public sealed class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventDetailsResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public GetEventByIdQueryHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<EventDetailsResponse> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        await using var connection = await _database.CreateConnectionAsync();

        var sql = $"""
            {EventSql.DetailsSelect}
            WHERE e.Id = @Id
            LIMIT 1;
            """;

        var eventDetails = await connection.QuerySingleOrDefaultAsync<EventDetailsResponse>(sql, new { request.Id });
        if (eventDetails is null)
        {
            throw new NotFoundException("Event not found.");
        }

        return eventDetails;
    }
}
