using Dapper;
using EventRegistration.Api.Interfaces;
using FluentValidation;
using MediatR;

namespace EventRegistration.Api.Features.Registrations;

public sealed record GetRegistrationsQuery(ulong EventId, int Page, int PageSize, string? Search, int? Status, ulong? ParticipantId) : IRequest<PagedRegistrationResponse>;

public sealed class GetRegistrationsQueryHandler : IRequestHandler<GetRegistrationsQuery, PagedRegistrationResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public GetRegistrationsQueryHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<PagedRegistrationResponse> Handle(GetRegistrationsQuery request, CancellationToken cancellationToken)
    {
        var offset = (request.Page - 1) * request.PageSize;

        await using var connection = await _database.CreateConnectionAsync();

        var whereClauses = new List<string> { "r.EventId = @EventId" };
        var parameters = new DynamicParameters();
        parameters.Add("EventId", request.EventId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            whereClauses.Add("(LOWER(e.Name) LIKE LOWER(CONCAT('%', @Search, '%')) OR LOWER(p.FullName) LIKE LOWER(CONCAT('%', @Search, '%')) OR LOWER(p.Email) LIKE LOWER(CONCAT('%', @Search, '%'))) ");
            parameters.Add("Search", request.Search);
        }

        if (request.Status.HasValue)
        {
            whereClauses.Add("r.Status = @Status");
            parameters.Add("Status", request.Status.Value);
        }

        if (request.ParticipantId.HasValue)
        {
            whereClauses.Add("r.ParticipantId = @ParticipantId");
            parameters.Add("ParticipantId", request.ParticipantId.Value);
        }

        var where = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : string.Empty;

        var countSql = $@"
            SELECT COUNT(*)
            FROM Registrations r
            INNER JOIN Events e ON r.EventId = e.Id
            INNER JOIN Participants p ON r.ParticipantId = p.Id
            {where}";

        var dataSql = $@"
            SELECT
                r.Id,
                r.EventId,
                e.Name AS EventName,
                r.ParticipantId,
                p.FullName AS ParticipantName,
                p.Email AS ParticipantEmail,
                p.Phone AS ParticipantPhone,
                r.Status,
                CASE r.Status WHEN 1 THEN 'Active' WHEN 2 THEN 'Cancelled' ELSE 'Unknown' END AS StatusName,
                r.Notes,
                r.RegisteredAt,
                r.CancelledAt
            FROM Registrations r
            INNER JOIN Events e ON r.EventId = e.Id
            INNER JOIN Participants p ON r.ParticipantId = p.Id
            {where}
            ORDER BY r.RegisteredAt DESC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", request.PageSize);
        parameters.Add("Offset", offset);

        var totalCount = await connection.ExecuteScalarAsync<long>(countSql, parameters);
        var rows = (await connection.QueryAsync<RegistrationResponse>(dataSql, parameters)).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedRegistrationResponse(rows, request.Page, request.PageSize, totalCount, totalPages);
    }
}

public sealed class GetRegistrationsQueryValidator : AbstractValidator<GetRegistrationsQuery>
{
    public GetRegistrationsQueryValidator()
    {
        RuleFor(x => x.EventId).NotEmpty().WithMessage("EventId is required.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page must be greater than or equal to 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
        RuleFor(x => x.Status).Must(x => x is null or 1 or 2).WithMessage("Status must be 1 or 2.");
    }
}
