using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using FluentValidation;
using MediatR;

namespace EventRegistration.Api.Features.Events;

public sealed record CreateEventCommand(
    string Name,
    string? Description,
    string Location,
    ulong CategoryId,
    int Capacity,
    DateTime StartAt,
    DateTime EndAt,
    DateTime RegistrationDeadline,
    bool IsActive) : IRequest<EventResponse>;

public sealed class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, EventResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public CreateEventCommandHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<EventResponse> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        var location = request.Location.Trim();

        await using var connection = await _database.CreateConnectionAsync();

        const string categorySql = """
            SELECT IsActive
            FROM Categories
            WHERE Id = @CategoryId
            LIMIT 1;
            """;

        var categoryIsActive = await connection.ExecuteScalarAsync<bool?>(categorySql, new { request.CategoryId });
        if (!categoryIsActive.HasValue)
        {
            throw new NotFoundException("Category not found.");
        }

        if (request.IsActive && !categoryIsActive.Value)
        {
            throw new BusinessException("Active events must use an active category.");
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
                UpdatedAt
            )
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
                NULL
            );

            SELECT LAST_INSERT_ID();
            """;

        var eventId = await connection.ExecuteScalarAsync<ulong>(
            insertSql,
            new
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

        return await connection.QuerySingleAsync<EventResponse>(EventSql.SelectById, new { Id = eventId });
    }
}

public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(150).WithMessage("Name must be 150 characters or less.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must be 1000 characters or less.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required.")
            .MaximumLength(200).WithMessage("Location must be 200 characters or less.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be greater than 0.");

        RuleFor(x => x.EndAt)
            .GreaterThan(x => x.StartAt).WithMessage("StartAt must be before EndAt.");

        RuleFor(x => x.RegistrationDeadline)
            .LessThan(x => x.StartAt).WithMessage("RegistrationDeadline must be before StartAt.");
    }
}
