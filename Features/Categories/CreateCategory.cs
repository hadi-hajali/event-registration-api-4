using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using FluentValidation;
using MediatR;

namespace EventRegistration.Api.Features.Categories;

public sealed record CreateCategoryCommand(string Name, string? Description, bool IsActive) : IRequest<CategoryResponse>;

public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public CreateCategoryCommandHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<CategoryResponse> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var trimmedName = request.Name?.Trim() ?? string.Empty;
        var trimmedDescription = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await using var connection = await _database.CreateConnectionAsync();

        const string duplicateSql = @"
            SELECT Id
            FROM Categories
            WHERE LOWER(TRIM(Name)) = LOWER(@Name)
            LIMIT 1";

        var duplicateId = await connection.ExecuteScalarAsync<long?>(duplicateSql, new { Name = trimmedName });
        if (duplicateId.HasValue)
        {
            throw new ConflictException("A category with that name already exists.");
        }

        const string insertSql = @"
            INSERT INTO Categories (Name, Description, IsActive, CreatedAt, UpdatedAt)
            VALUES (@Name, @Description, @IsActive, UTC_TIMESTAMP(), NULL);
            SELECT LAST_INSERT_ID();";

        var categoryId = await connection.ExecuteScalarAsync<long>(insertSql, new
        {
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

        var createdCategory = await connection.QuerySingleOrDefaultAsync<CategoryResponse>(selectSql, new { Id = categoryId });
        if (createdCategory is null)
        {
            throw new NotFoundException("Category not found after creation.");
        }

        return createdCategory;
    }
}

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
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
