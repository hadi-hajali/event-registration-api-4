using Dapper;
using EventRegistration.Api.Interfaces;
using FluentValidation;
using MediatR;

namespace EventRegistration.Api.Features.Events;

public sealed record EventPagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount,
    int TotalPages);

public sealed record GetEventsQuery(
    int Page,
    int PageSize,
    string? Search,
    ulong? CategoryId,
    DateTime? FromDate,
    DateTime? ToDate,
    bool? IsActive) : IRequest<EventPagedResult<EventListItem>>;

public sealed class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, EventPagedResult<EventListItem>>
{
    private readonly IEventRegistrationDatabase _database;

    public GetEventsQueryHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<EventPagedResult<EventListItem>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var offset = (page - 1) * pageSize;
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : $"%{request.Search.Trim()}%";

        const string whereSql = """
            WHERE
                (@Search IS NULL OR e.Name LIKE @Search OR e.Location LIKE @Search)
                AND (@CategoryId IS NULL OR e.CategoryId = @CategoryId)
                AND (@FromDate IS NULL OR e.StartAt >= @FromDate)
                AND (@ToDate IS NULL OR e.StartAt <= @ToDate)
                AND (@IsActive IS NULL OR e.IsActive = @IsActive)
            """;

        var parameters = new
        {
            Search = search,
            request.CategoryId,
            request.FromDate,
            request.ToDate,
            request.IsActive,
            PageSize = pageSize,
            Offset = offset
        };

        const string countSql = $"""
            SELECT COUNT(*)
            FROM Events e
            INNER JOIN Categories c ON e.CategoryId = c.Id
            {whereSql};
            """;

        var dataSql = $"""
            {EventSql.ListSelect}
            {whereSql}
            ORDER BY e.StartAt ASC, e.Id ASC
            LIMIT @PageSize OFFSET @Offset;
            """;

        await using var connection = await _database.CreateConnectionAsync();

        var totalCount = await connection.ExecuteScalarAsync<long>(countSql, parameters);
        var items = (await connection.QueryAsync<EventListItem>(dataSql, parameters)).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new EventPagedResult<EventListItem>(items, page, pageSize, totalCount, totalPages);
    }
}

public sealed class GetEventsQueryValidator : AbstractValidator<GetEventsQuery>
{
    public GetEventsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x)
            .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
            .WithMessage("FromDate must be earlier than or equal to ToDate.");
    }
}
