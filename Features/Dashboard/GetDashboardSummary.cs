using Dapper;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Dashboard;

public sealed record GetDashboardSummaryQuery : IRequest<DashboardSummary>;

public sealed class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummary>
{
    private readonly IEventRegistrationDatabase _database;

    public GetDashboardSummaryQueryHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<DashboardSummary> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        await using var connection = await _database.CreateConnectionAsync();

        const string countsSql = """
            SELECT
                (SELECT COUNT(*) FROM Categories WHERE IsActive = 1) AS TotalActiveCategories,
                (SELECT COUNT(*) FROM Events WHERE IsActive = 1) AS TotalActiveEvents,
                (SELECT COUNT(*) FROM Participants WHERE IsActive = 1) AS TotalActiveParticipants,
                (SELECT COUNT(*) FROM Registrations WHERE Status = 1) AS TotalActiveRegistrations;
            """;

        const string upcomingEventsSql = """
            SELECT
                e.Id,
                e.Name AS Title,
                c.Name AS CategoryName,
                e.StartAt AS StartDate,
                e.Location,
                e.Capacity,
                COUNT(r.Id) AS RegisteredCount
            FROM Events e
            INNER JOIN Categories c ON c.Id = e.CategoryId
            LEFT JOIN Registrations r ON r.EventId = e.Id AND r.Status = 1
            WHERE e.IsActive = 1
              AND e.StartAt >= UTC_DATE()
            GROUP BY e.Id, e.Name, c.Name, e.StartAt, e.Location, e.Capacity
            ORDER BY e.StartAt ASC
            LIMIT 5;
            """;

        var counts = await connection.QuerySingleAsync<DashboardCounts>(countsSql);
        var upcomingEvents = (await connection.QueryAsync<UpcomingEvent>(upcomingEventsSql)).ToList();

        return new DashboardSummary(
            counts.TotalActiveCategories,
            counts.TotalActiveEvents,
            counts.TotalActiveParticipants,
            counts.TotalActiveRegistrations,
            upcomingEvents);
    }
}

internal sealed record DashboardCounts(
    long TotalActiveCategories,
    long TotalActiveEvents,
    long TotalActiveParticipants,
    long TotalActiveRegistrations);
