using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Categories;

public sealed record GetCategoryByIdQuery(long Id) : IRequest<CategoryResponse>;

public sealed class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, CategoryResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public GetCategoryByIdQueryHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<CategoryResponse> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        await using var connection = await _database.CreateConnectionAsync();

        const string sql = @"
            SELECT
                Id,
                Name,
                Description,
                IsActive,
                CreatedAt,
                UpdatedAt
            FROM Categories
            WHERE Id = @Id";

        var category = await connection.QuerySingleOrDefaultAsync<CategoryResponse>(sql, new { Id = request.Id });
        if (category is null)
        {
            throw new NotFoundException("Category not found.");
        }

        return category;
    }
}
