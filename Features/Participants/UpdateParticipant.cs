using Dapper;
using MediatR;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;

namespace EventRegistration.Api.Features.Participants;

public static class UpdateParticipant
{
    public sealed record Request(
        string FullName,
        string Email,
        string Phone,
        DateTime? DateOfBirth,
        bool IsActive
    );

    public sealed record Command(
        long Id,
        string FullName,
        string Email,
        string Phone,
        DateTime? DateOfBirth,
        bool IsActive
    ) : IRequest<ParticipantDto>;

    public sealed class Handler : IRequestHandler<Command, ParticipantDto>
    {
        private readonly IEventRegistrationDatabase _database;

        public Handler(IEventRegistrationDatabase database)
        {
            _database = database;
        }

        public async Task<ParticipantDto> Handle(Command request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                throw new AppValidationException("Invalid input.", ["Full name is required."]);
            }

            if (request.FullName.Trim().Length > 150)
            {
                throw new AppValidationException("Invalid input.", ["Full name maximum length is 150."]);
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new AppValidationException("Invalid input.", ["Email is required."]);
            }

            if (request.Email.Trim().Length > 255)
            {
                throw new AppValidationException("Invalid input.", ["Email maximum length is 255."]);
            }

            if (string.IsNullOrWhiteSpace(request.Phone))
            {
                throw new AppValidationException("Invalid input.", ["Phone is required."]);
            }

            if (request.Phone.Trim().Length > 30)
            {
                throw new AppValidationException("Invalid input.", ["Phone maximum length is 30."]);
            }

            if (request.DateOfBirth is not null && request.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
            {
                throw new AppValidationException("Invalid input.", ["Date of birth cannot be in the future."]);
            }

            var fullName = request.FullName.Trim();
            var email = request.Email.Trim().ToLowerInvariant();
            var phone = request.Phone.Trim();

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

            const string duplicateSql = """
                SELECT COUNT(*)
                FROM Participants
                WHERE LOWER(TRIM(Email)) = @Email
                  AND Id <> @Id;
                """;

            var duplicateCount = await connection.ExecuteScalarAsync<int>(
                duplicateSql,
                new
                {
                    Email = email,
                    request.Id
                });

            if (duplicateCount > 0)
            {
                throw new DuplicateResourceException("Participant email already exists.");
            }

            const string updateSql = """
                UPDATE Participants
                SET
                    FullName = @FullName,
                    Email = @Email,
                    Phone = @Phone,
                    DateOfBirth = @DateOfBirth,
                    IsActive = @IsActive,
                    UpdatedAt = UTC_TIMESTAMP()
                WHERE Id = @Id;
                """;

            await connection.ExecuteAsync(
                updateSql,
                new
                {
                    request.Id,
                    FullName = fullName,
                    Email = email,
                    Phone = phone,
                    DateOfBirth = request.DateOfBirth?.Date,
                    request.IsActive
                });

            const string selectSql = """
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

            return await connection.QuerySingleAsync<ParticipantDto>(
                selectSql,
                new { request.Id });
        }
    }
}