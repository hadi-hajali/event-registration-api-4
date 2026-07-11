using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Events;

public sealed record SetEventActiveStateRequest(bool IsActive);

public sealed record SetEventActiveStateCommand(ulong Id, bool IsActive) : IRequest<EventDetailsResponse>;

public sealed class SetEventActiveStateCommandHandler : IRequestHandler<SetEventActiveStateCommand, EventDetailsResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public SetEventActiveStateCommandHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<EventDetailsResponse> Handle(SetEventActiveStateCommand request, CancellationToken cancellationToken)
    {
        await using var connection = await _database.CreateConnectionAsync();

        const string updateSql = """
            UPDATE Events
            SET IsActive = @IsActive,
                UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id;
            """;

        var affectedRows = await connection.ExecuteAsync(updateSql, new { request.Id, request.IsActive });
        if (affectedRows == 0)
        {
            throw new NotFoundException("Event not found.");
        }

        var selectSql = $"""
            {EventSql.DetailsSelect}
            WHERE e.Id = @Id
            LIMIT 1;
            """;

        var updatedEvent = await connection.QuerySingleOrDefaultAsync<EventDetailsResponse>(selectSql, new { request.Id });
        if (updatedEvent is null)
        {
            throw new NotFoundException("Event not found after active state update.");
        }

        return updatedEvent;
    }
}
