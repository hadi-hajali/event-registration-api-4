using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using FluentValidation;
using MediatR;
using MySqlConnector;

namespace EventRegistration.Api.Features.Registrations;

public sealed record CreateRegistrationCommand(ulong EventId, ulong ParticipantId, string? Notes) : IRequest<RegistrationResponse>;

public sealed class CreateRegistrationCommandHandler : IRequestHandler<CreateRegistrationCommand, RegistrationResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public CreateRegistrationCommandHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<RegistrationResponse> Handle(CreateRegistrationCommand request, CancellationToken cancellationToken)
    {
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        await using var connection = await _database.CreateConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string eventSql = @"
            SELECT Id, Capacity, IsActive, RegistrationDeadline, StartAt
            FROM Events
            WHERE Id = @EventId
            LIMIT 1
            FOR UPDATE";

        var eventInfo = await connection.QuerySingleOrDefaultAsync<EventRegistrationState>(
            eventSql,
            new { EventId = request.EventId },
            transaction);

        if (eventInfo is null)
            throw new NotFoundException("Event not found.");

        if (!eventInfo.IsActive)
            throw new BusinessException("Event is not active.");

        if (eventInfo.RegistrationDeadline <= DateTime.UtcNow)
            throw new BusinessException("Registration deadline has passed.");

        if (eventInfo.StartAt <= DateTime.UtcNow)
            throw new BusinessException("Event has already started.");

        const string participantSql = @"
            SELECT Id, IsActive
            FROM Participants
            WHERE Id = @ParticipantId
            LIMIT 1";

        var participant = await connection.QuerySingleOrDefaultAsync<ParticipantRegistrationState>(
            participantSql,
            new { ParticipantId = request.ParticipantId },
            transaction);

        if (participant is null)
            throw new NotFoundException("Participant not found.");

        if (!participant.IsActive)
            throw new BusinessException("Participant is not active.");

        const string existingSql = @"
            SELECT Id, Status
            FROM Registrations
            WHERE EventId = @EventId AND ParticipantId = @ParticipantId
            LIMIT 1";

        var existing = await connection.QuerySingleOrDefaultAsync<ExistingRegistrationState>(
            existingSql,
            new { EventId = request.EventId, ParticipantId = request.ParticipantId },
            transaction);

        if (existing?.Status == 1)
            throw new DuplicateResourceException("An active registration for this participant and event already exists.");

        const string activeCountSql = @"
            SELECT COUNT(*)
            FROM Registrations
            WHERE EventId = @EventId AND Status = 1";

        var activeCount = await connection.ExecuteScalarAsync<int>(
            activeCountSql,
            new { EventId = request.EventId },
            transaction);

        if (activeCount >= eventInfo.Capacity)
            throw new BusinessException("Event capacity is full.");

        ulong registrationId;

        try
        {
            if (existing?.Status == 2)
            {
                const string reactivateSql = @"
                    UPDATE Registrations
                    SET Status = 1,
                        Notes = @Notes,
                        RegisteredAt = UTC_TIMESTAMP(),
                        CancelledAt = NULL
                    WHERE Id = @Id";

                await connection.ExecuteAsync(
                    reactivateSql,
                    new { existing.Id, Notes = notes },
                    transaction);

                registrationId = existing.Id;
            }
            else
            {
                const string insertSql = @"
                    INSERT INTO Registrations (EventId, ParticipantId, Status, Notes, RegisteredAt, CancelledAt)
                    VALUES (@EventId, @ParticipantId, 1, @Notes, UTC_TIMESTAMP(), NULL);
                    SELECT LAST_INSERT_ID();";

                registrationId = await connection.ExecuteScalarAsync<ulong>(
                    insertSql,
                    new { EventId = request.EventId, ParticipantId = request.ParticipantId, Notes = notes },
                    transaction);
            }
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new DuplicateResourceException("An active registration for this participant and event already exists.");
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
            WHERE r.Id = @Id";

        var created = await connection.QuerySingleOrDefaultAsync<RegistrationResponse>(
            selectSql,
            new { Id = registrationId },
            transaction);

        if (created is null)
            throw new NotFoundException("Registration not found after creation.");

        await transaction.CommitAsync(cancellationToken);
        return created;
    }

    private sealed record EventRegistrationState(ulong Id, int Capacity, bool IsActive, DateTime RegistrationDeadline, DateTime StartAt);

    private sealed record ParticipantRegistrationState(ulong Id, bool IsActive);

    private sealed record ExistingRegistrationState(ulong Id, int Status);
}

public sealed class CreateRegistrationCommandValidator : AbstractValidator<CreateRegistrationCommand>
{
    public CreateRegistrationCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty().WithMessage("EventId is required.");
        RuleFor(x => x.ParticipantId).NotEmpty().WithMessage("ParticipantId is required.");
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage("Notes must be 500 characters or less.");
    }
}
