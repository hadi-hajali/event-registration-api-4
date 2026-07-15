using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using FluentValidation;
using MediatR;

namespace EventRegistration.Api.Features.Events;

public sealed record UpdateEventCommand(
    ulong Id,
    string Name,
    string? Description,
    string Location,
    ulong CategoryId,
    int Capacity,
    DateTime StartAt,
    DateTime EndAt,
    DateTime RegistrationDeadline,
    bool IsActive) : IRequest<EventResponse>;

public sealed class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, EventResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public UpdateEventCommandHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<EventResponse> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        var location = request.Location.Trim();

        await using var connection = await _database.CreateConnectionAsync();

        const string existsSql = """
            SELECT COUNT(*)
            FROM Events
            WHERE Id = @Id;
            """;

        var exists = await connection.ExecuteScalarAsync<int>(existsSql, new { request.Id });
        if (exists == 0)
        {
            throw new NotFoundException("Event not found.");
        }

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

        const string updateSql = """
            UPDATE Events
            SET
                CategoryId = @CategoryId,
                Name = @Name,
                Description = @Description,
                Location = @Location,
                StartAt = @StartAt,
                EndAt = @EndAt,
                RegistrationDeadline = @RegistrationDeadline,
                Capacity = @Capacity,
                IsActive = @IsActive,
                UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(
            updateSql,
            new
            {
                request.Id,
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

        return await connection.QuerySingleAsync<EventResponse>(EventSql.SelectById, new { request.Id });
    }
}

public sealed class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Event id is required.");

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
