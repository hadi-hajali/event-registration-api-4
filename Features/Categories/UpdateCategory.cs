using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using FluentValidation;
using MediatR;

namespace EventRegistration.Api.Features.Categories;

public sealed record UpdateCategoryCommand(long Id, string Name, string? Description, bool IsActive) : IRequest<CategoryResponse>;

public sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public UpdateCategoryCommandHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<CategoryResponse> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var trimmedName = request.Name.Trim();
        var trimmedDescription = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await using var connection = await _database.CreateConnectionAsync();

        const string existingSql = @"
            SELECT Id
            FROM Categories
            WHERE Id = @Id";

        var existingId = await connection.ExecuteScalarAsync<long?>(existingSql, new { Id = request.Id });
        if (!existingId.HasValue)
        {
            throw new NotFoundException("Category not found.");
        }

        const string duplicateSql = @"
            SELECT Id
            FROM Categories
            WHERE Id <> @Id
              AND LOWER(TRIM(Name)) = LOWER(@Name)
            LIMIT 1";

        var duplicateId = await connection.ExecuteScalarAsync<long?>(duplicateSql, new { Id = request.Id, Name = trimmedName });
        if (duplicateId.HasValue)
        {
            throw new ConflictException("A category with that name already exists.");
        }

        const string updateSql = @"
            UPDATE Categories
            SET Name = @Name,
                Description = @Description,
                IsActive = @IsActive,
                UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id";

        await connection.ExecuteAsync(updateSql, new
        {
            Id = request.Id,
            Name = trimmedName,
            Description = trimmedDescription,
            IsActive = request.IsActive
        });

        const string selectSql = @"
            SELECT
                Id,
                Name,
                Description,
                IsActive,
                CreatedAt,
                UpdatedAt
            FROM Categories
            WHERE Id = @Id";

        var updatedCategory = await connection.QuerySingleOrDefaultAsync<CategoryResponse>(selectSql, new { Id = request.Id });
        if (updatedCategory is null)
        {
            throw new NotFoundException("Category not found after update.");
        }

        return updatedCategory;
    }
}

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Name is required.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Name)
                    .Must(name => name is not null && name.Trim().Length is >= 2 and <= 100)
                    .WithMessage("Name length must be between 2 and 100 characters.");
            });

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must be 500 characters or less.");
    }
}
