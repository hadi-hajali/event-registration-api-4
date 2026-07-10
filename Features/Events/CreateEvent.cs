using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using FluentValidation;
using MediatR;

namespace EventRegistration.Api.Features.Events;

public sealed record CreateEventCommand(
    ulong CategoryId,
    string Name,
    string? Description,
    string Location,
    DateTime StartAt,
    DateTime EndAt,
    DateTime RegistrationDeadline,
    int Capacity,
    bool IsActive) : IRequest<EventDetailsResponse>;

public sealed class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, EventDetailsResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public CreateEventCommandHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<EventDetailsResponse> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        var location = request.Location.Trim();

        await using var connection = await _database.CreateConnectionAsync();

        const string categorySql = """
            SELECT Id
            FROM Categories
            WHERE Id = @CategoryId AND IsActive = TRUE
            LIMIT 1;
            """;

        var categoryId = await connection.ExecuteScalarAsync<ulong?>(categorySql, new { request.CategoryId });
        if (!categoryId.HasValue)
        {
            throw new EventRegistration.Api.Exceptions.ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(nameof(request.CategoryId), "Category must exist and be active.")
            });
        }

        const string insertSql = """
            INSERT INTO Events (
                CategoryId,
                Name,
                Description,
                Location,
                StartAt,
                EndAt,
                RegistrationDeadline,
                Capacity,
                IsActive,
                CreatedAt,
                UpdatedAt)
            VALUES (
                @CategoryId,
                @Name,
                @Description,
                @Location,
                @StartAt,
                @EndAt,
                @RegistrationDeadline,
                @Capacity,
                @IsActive,
                UTC_TIMESTAMP(),
                NULL);
            SELECT LAST_INSERT_ID();
            """;

        var eventId = await connection.ExecuteScalarAsync<ulong>(insertSql, new
        {
            request.CategoryId,
            Name = name,
            Description = description,
            Location = location,
            request.StartAt,
            request.EndAt,
            request.RegistrationDeadline,
            request.Capacity,
            request.IsActive
        });

        var selectSql = $"""
            {EventSql.DetailsSelect}
            WHERE e.Id = @Id
            LIMIT 1;
            """;

        var createdEvent = await connection.QuerySingleOrDefaultAsync<EventDetailsResponse>(selectSql, new { Id = eventId });
        if (createdEvent is null)
        {
            throw new NotFoundException("Event not found after creation.");
        }

        return createdEvent;
    }
}

public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Name is required.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Name.Trim())
                    .MaximumLength(150)
                    .WithMessage("Name must be 150 characters or less.");
            });

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description must be 1000 characters or less.");

        RuleFor(x => x.Location)
            .NotNull()
            .Must(location => !string.IsNullOrWhiteSpace(location))
            .WithMessage("Location is required.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Location.Trim())
                    .MaximumLength(200)
                    .WithMessage("Location must be 200 characters or less.");
            });

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("CategoryId is required.");

        RuleFor(x => x.Capacity)
            .InclusiveBetween(1, 10000)
            .WithMessage("Capacity must be between 1 and 10,000.");

        RuleFor(x => x)
            .Must(x => x.EndAt > x.StartAt)
            .WithMessage("EndAt must be later than StartAt.");

        RuleFor(x => x)
            .Must(x => x.RegistrationDeadline <= x.StartAt)
            .WithMessage("RegistrationDeadline must not be later than StartAt.");
    }
}
