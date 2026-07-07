using Dapper;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Categories;

public sealed record GetCategoriesQuery(bool IncludeInactive) : IRequest<IReadOnlyList<CategoryResponse>>;

public sealed class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryResponse>>
{
    private readonly IEventRegistrationDatabase _database;

    public GetCategoriesQueryHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<CategoryResponse>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        await using var connection = await _database.CreateConnectionAsync();

        var sql = request.IncludeInactive
            ? @"SELECT
                    Id,
                    Name,
                    Description,
                    IsActive,
                    CreatedAt,
                    UpdatedAt
                FROM Categories
                ORDER BY Name ASC"
            : @"SELECT
                    Id,
                    Name,
                    Description,
                    IsActive,
                    CreatedAt,
                    UpdatedAt
                FROM Categories
                WHERE IsActive = TRUE
                ORDER BY Name ASC";

        var categories = await connection.QueryAsync<CategoryResponse>(sql);
        return categories.ToList();
    }
}
