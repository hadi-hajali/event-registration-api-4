using Dapper;
using MediatR;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;

namespace EventRegistration.Api.Features.Participants;

public static class GetParticipantById
{
    public sealed record Query(long Id) : IRequest<ParticipantDto>;

    public sealed class Handler : IRequestHandler<Query, ParticipantDto>
    {
        private readonly IEventRegistrationDatabase _database;

        public Handler(IEventRegistrationDatabase database)
        {
            _database = database;
        }

        public async Task<ParticipantDto> Handle(Query request, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    Id,
                    FullName,
                    Email,
                    Phone,
                    DateOfBirth,
                    IsActive,
                    CreatedAt,
                    UpdatedAt
                FROM Participants
                WHERE Id = @Id;
                """;

            await using var connection = await _database.CreateConnectionAsync();

            var participant = await connection.QuerySingleOrDefaultAsync<ParticipantDto>(
                sql,
                new { request.Id });

            if (participant is null)
            {
                throw new NotFoundException("Participant was not found.");
            }

            return participant;
        }
    }
}