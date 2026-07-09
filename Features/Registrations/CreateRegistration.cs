using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using FluentValidation;
using MediatR;

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

        const string eventSql = @"SELECT Id FROM Events WHERE Id = @EventId LIMIT 1";
        var eventId = await connection.ExecuteScalarAsync<ulong?>(eventSql, new { EventId = request.EventId });
        if (!eventId.HasValue)
            throw new NotFoundException("Event not found.");

        const string participantSql = @"SELECT Id FROM Participants WHERE Id = @ParticipantId LIMIT 1";
        var participantId = await connection.ExecuteScalarAsync<ulong?>(participantSql, new { ParticipantId = request.ParticipantId });
        if (!participantId.HasValue)
            throw new NotFoundException("Participant not found.");

        const string duplicateSql = @"
            SELECT Id FROM Registrations
            WHERE EventId = @EventId AND ParticipantId = @ParticipantId AND Status = 1
            LIMIT 1";

        var dup = await connection.ExecuteScalarAsync<ulong?>(duplicateSql, new { EventId = request.EventId, ParticipantId = request.ParticipantId });
        if (dup.HasValue)
            throw new ConflictException("An active registration for this participant and event already exists.");

        const string insertSql = @"
            INSERT INTO Registrations (EventId, ParticipantId, Status, Notes, RegisteredAt, CancelledAt)
            VALUES (@EventId, @ParticipantId, 1, @Notes, UTC_TIMESTAMP(), NULL);
            SELECT LAST_INSERT_ID();";

        var newId = await connection.ExecuteScalarAsync<ulong>(insertSql, new { EventId = request.EventId, ParticipantId = request.ParticipantId, Notes = notes });

        const string selectSql = @"
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

        var created = await connection.QuerySingleOrDefaultAsync<RegistrationResponse>(selectSql, new { Id = newId });
        if (created is null)
            throw new NotFoundException("Registration not found after creation.");

        return created;
    }
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
