using Dapper;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Dashboard;

public sealed class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public GetDashboardSummaryQueryHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<DashboardSummaryResponse> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        await using var connection = await _database.CreateConnectionAsync();

        const string countsSql = """
            SELECT COUNT(*)
            FROM Categories
            WHERE IsActive = TRUE;

            SELECT COUNT(*)
            FROM Participants
            WHERE IsActive = TRUE;

            SELECT COUNT(*)
            FROM Events
            WHERE IsActive = TRUE
              AND StartAt > UTC_TIMESTAMP();

            SELECT COUNT(*)
            FROM Registrations
            WHERE Status = 1;
            """;

        using var multipleResults = await connection.QueryMultipleAsync(countsSql);

        var totalActiveCategories =
            await multipleResults.ReadSingleAsync<int>();

        var totalActiveParticipants =
            await multipleResults.ReadSingleAsync<int>();

        var totalUpcomingEvents =
            await multipleResults.ReadSingleAsync<int>();

        var totalActiveRegistrations =
            await multipleResults.ReadSingleAsync<int>();

        const string upcomingEventsSql = """
            SELECT
                e.Id,
                e.Name,
                c.Name AS CategoryName,
                e.StartAt,
                e.Location,
                e.Capacity,
                COUNT(r.Id) AS ActiveRegistrationCount,
                e.Capacity - COUNT(r.Id) AS AvailableSeats
            FROM Events e
            INNER JOIN Categories c
                ON c.Id = e.CategoryId
            LEFT JOIN Registrations r
                ON r.EventId = e.Id
                AND r.Status = 1
            WHERE e.IsActive = TRUE
              AND e.StartAt > UTC_TIMESTAMP()
            GROUP BY
                e.Id,
                e.Name,
                c.Name,
                e.StartAt,
                e.Location,
                e.Capacity
            ORDER BY e.StartAt ASC
            LIMIT 5;
            """;

        var upcomingEvents = await connection.QueryAsync<UpcomingEventResponse>(
            upcomingEventsSql);

        return new DashboardSummaryResponse
        {
            TotalActiveCategories = totalActiveCategories,
            TotalActiveParticipants = totalActiveParticipants,
            TotalUpcomingEvents = totalUpcomingEvents,
            TotalActiveRegistrations = totalActiveRegistrations,
            UpcomingEvents = upcomingEvents.ToList()
        };
    }
}