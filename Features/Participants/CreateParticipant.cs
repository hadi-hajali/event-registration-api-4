using Dapper;
using FluentValidation;
using MediatR;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using MySqlConnector;

namespace EventRegistration.Api.Features.Participants;

public static class CreateParticipant
{
    public sealed record Command(
        string FullName,
        string Email,
        string? Phone,
        DateTime? DateOfBirth,
        bool? IsActive
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
            var phone = request.Phone?.Trim() ?? string.Empty;
            var isActive = request.IsActive ?? true;

            await using var connection = await _database.CreateConnectionAsync();

            const string duplicateSql = """
                SELECT COUNT(*)
                FROM Participants
                WHERE LOWER(TRIM(Email)) = @Email;
                """;

            var duplicateCount = await connection.ExecuteScalarAsync<int>(
                duplicateSql,
                new { Email = email });

            if (duplicateCount > 0)
            {
                throw new DuplicateResourceException("Participant email already exists.");
            }

            const string insertSql = """
                INSERT INTO Participants (
                    FullName,
                    Email,
                    Phone,
                    DateOfBirth,
                    IsActive,
                    CreatedAt
                )
                VALUES (
                    @FullName,
                    @Email,
                    @Phone,
                    @DateOfBirth,
                    @IsActive,
                    UTC_TIMESTAMP()
                );

                SELECT LAST_INSERT_ID();
                """;

            ulong id;

            try
            {
                id = await connection.ExecuteScalarAsync<ulong>(
                    insertSql,
                    new
                    {
                        FullName = fullName,
                        Email = email,
                        Phone = phone,
                        DateOfBirth = request.DateOfBirth?.Date,
                        IsActive = isActive
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
                new { Id = id });
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(150).WithMessage("Full name maximum length is 150.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.")
                .MaximumLength(255).WithMessage("Email maximum length is 255.");

            RuleFor(x => x.Phone)
                .MaximumLength(30).WithMessage("Phone maximum length is 30.")
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.DateOfBirth)
                .Must(dateOfBirth => dateOfBirth is null || dateOfBirth.Value.Date <= DateTime.UtcNow.Date)
                .WithMessage("Date of birth cannot be in the future.");
        }
    }
}
