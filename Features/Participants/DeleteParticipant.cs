using Dapper;
using MediatR;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;

namespace EventRegistration.Api.Features.Participants;

public static class DeleteParticipant
{
    public sealed record Command(long Id) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IEventRegistrationDatabase _database;

        public Handler(IEventRegistrationDatabase database)
        {
            _database = database;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            const string existsSql = """
                SELECT COUNT(*)
                FROM Participants
                WHERE Id = @Id;
                """;

            var exists = await connection.ExecuteScalarAsync<int>(
                existsSql,
                new { request.Id });

            if (exists == 0)
            {
                throw new NotFoundException("Participant was not found.");
            }

            const string registrationsSql = """
                SELECT COUNT(*)
                FROM Registrations
                WHERE ParticipantId = @Id;
                """;

            var registrationsCount = await connection.ExecuteScalarAsync<int>(
                registrationsSql,
                new { request.Id });

            if (registrationsCount > 0)
            {
                throw new BusinessException("Participant cannot be deleted because they have registration history.");
            }

            const string deleteSql = """
                DELETE FROM Participants
                WHERE Id = @Id;
                """;

            await connection.ExecuteAsync(deleteSql, new { request.Id });
        }
    }
}