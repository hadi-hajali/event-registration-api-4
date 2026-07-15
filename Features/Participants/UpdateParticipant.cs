using Dapper;
using FluentValidation;
using MediatR;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using MySqlConnector;

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
            var fullName = request.FullName.Trim();
            var email = request.Email.Trim().ToLowerInvariant();
            var phone = request.Phone.Trim();

            await using var connection = await _database.CreateConnectionAsync();

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

            try
            {
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
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                throw new DuplicateResourceException("Participant email already exists.");
            }

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

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Participant id is required.");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(150).WithMessage("Full name maximum length is 150.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.")
                .MaximumLength(255).WithMessage("Email maximum length is 255.");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Phone is required.")
                .MaximumLength(30).WithMessage("Phone maximum length is 30.");

            RuleFor(x => x.DateOfBirth)
                .Must(dateOfBirth => dateOfBirth is null || dateOfBirth.Value.Date <= DateTime.UtcNow.Date)
                .WithMessage("Date of birth cannot be in the future.");
        }
    }
}
