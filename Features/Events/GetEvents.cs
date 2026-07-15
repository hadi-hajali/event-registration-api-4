using Dapper;
using EventRegistration.Api.Interfaces;
using FluentValidation;
using MediatR;

namespace EventRegistration.Api.Features.Events;

public sealed record GetEventsQuery(
    int Page,
    int PageSize,
    string? Search,
    ulong? CategoryId,
    bool? IsActive) : IRequest<PagedEventResponse>;

public sealed class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, PagedEventResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public GetEventsQueryHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<PagedEventResponse> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var offset = (request.Page - 1) * request.PageSize;
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            whereClauses.Add("(LOWER(e.Name) LIKE LOWER(CONCAT('%', @Search, '%')) OR LOWER(e.Location) LIKE LOWER(CONCAT('%', @Search, '%')) OR LOWER(c.Name) LIKE LOWER(CONCAT('%', @Search, '%')))");
            parameters.Add("Search", request.Search.Trim());
        }

        if (request.CategoryId.HasValue)
        {
            whereClauses.Add("e.CategoryId = @CategoryId");
            parameters.Add("CategoryId", request.CategoryId.Value);
        }

        if (request.IsActive.HasValue)
        {
            whereClauses.Add("e.IsActive = @IsActive");
            parameters.Add("IsActive", request.IsActive.Value);
        }

        var where = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : string.Empty;

        var countSql = $"""
            SELECT COUNT(*)
            FROM Events e
            INNER JOIN Categories c ON c.Id = e.CategoryId
            {where};
            """;

        var dataSql = $"""
            SELECT
                e.Id,
                e.Name,
                e.Description,
                e.Location,
                e.CategoryId,
                c.Name AS CategoryName,
                e.Capacity,
                e.StartAt,
                e.EndAt,
                e.RegistrationDeadline,
                e.IsActive,
                e.CreatedAt,
                e.UpdatedAt,
                COUNT(r.Id) AS RegisteredCount
            FROM Events e
            INNER JOIN Categories c ON c.Id = e.CategoryId
            LEFT JOIN Registrations r ON r.EventId = e.Id AND r.Status = 1
            {where}
            GROUP BY
                e.Id,
                e.Name,
                e.Description,
                e.Location,
                e.CategoryId,
                c.Name,
                e.Capacity,
                e.StartAt,
                e.EndAt,
                e.RegistrationDeadline,
                e.IsActive,
                e.CreatedAt,
                e.UpdatedAt
            ORDER BY e.StartAt ASC
            LIMIT @PageSize OFFSET @Offset;
            """;

        parameters.Add("PageSize", request.PageSize);
        parameters.Add("Offset", offset);

        await using var connection = await _database.CreateConnectionAsync();

        var totalCount = await connection.ExecuteScalarAsync<long>(countSql, parameters);
        var items = (await connection.QueryAsync<EventResponse>(dataSql, parameters)).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedEventResponse(items, request.Page, request.PageSize, totalCount, totalPages);
    }
}

public sealed class GetEventsQueryValidator : AbstractValidator<GetEventsQuery>
{
    public GetEventsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
    }
}
