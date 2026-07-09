using Dapper;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Registrations;

public sealed record GetRegistrationsQuery(int Page, int PageSize, string? Search, int? Status, ulong? EventId, ulong? ParticipantId) : IRequest<IReadOnlyList<RegistrationResponse>>;

public sealed class GetRegistrationsQueryHandler : IRequestHandler<GetRegistrationsQuery, IReadOnlyList<RegistrationResponse>>
{
    private readonly IEventRegistrationDatabase _database;

    public GetRegistrationsQueryHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<RegistrationResponse>> Handle(GetRegistrationsQuery request, CancellationToken cancellationToken)
    {
        var offset = Math.Max(0, request.Page - 1) * Math.Max(1, request.PageSize);

        await using var connection = await _database.CreateConnectionAsync();

        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

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

        if (request.EventId.HasValue)
        {
            whereClauses.Add("r.EventId = @EventId");
            parameters.Add("EventId", request.EventId.Value);
        }

        if (request.ParticipantId.HasValue)
        {
            whereClauses.Add("r.ParticipantId = @ParticipantId");
            parameters.Add("ParticipantId", request.ParticipantId.Value);
        }

        var where = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : string.Empty;

        var sql = $@"
            SELECT
                r.Id,
                r.EventId,
                e.Name AS EventName,
                r.ParticipantId,
                p.FullName AS ParticipantName,
                p.Email AS ParticipantEmail,
                r.Status,
                r.Notes,
                r.RegisteredAt,
                r.CancelledAt
            FROM Registrations r
            INNER JOIN Events e ON r.EventId = e.Id
            INNER JOIN Participants p ON r.ParticipantId = p.Id
            {where}
            ORDER BY r.RegisteredAt DESC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", Math.Max(1, request.PageSize));
        parameters.Add("Offset", offset);

        var rows = await connection.QueryAsync<RegistrationResponse>(sql, parameters);
        return rows.ToList();
    }
}
