namespace EventRegistration.Api.Features.Events;

internal static class EventSql
{
    public const string ListSelect = """
        SELECT
            e.Id,
            e.Name,
            e.CategoryId,
            c.Name AS CategoryName,
            e.Location,
            e.StartAt,
            e.EndAt,
            e.Capacity,
            COALESCE(activeRegistrations.ActiveRegistrationCount, 0) AS ActiveRegistrationCount,
            e.Capacity - COALESCE(activeRegistrations.ActiveRegistrationCount, 0) AS AvailableSeats,
            CASE
                WHEN UTC_TIMESTAMP() < e.StartAt THEN 'Upcoming'
                WHEN UTC_TIMESTAMP() >= e.EndAt THEN 'Completed'
                ELSE 'Ongoing'
            END AS EventStatus,
            e.IsActive
        FROM Events e
        INNER JOIN Categories c ON e.CategoryId = c.Id
        LEFT JOIN (
            SELECT EventId, COUNT(*) AS ActiveRegistrationCount
            FROM Registrations
            WHERE Status = 1
            GROUP BY EventId
        ) activeRegistrations ON activeRegistrations.EventId = e.Id
        """;

    public const string DetailsSelect = """
        SELECT
            e.Id,
            e.Name,
            e.Description,
            e.CategoryId,
            c.Name AS CategoryName,
            e.Location,
            e.StartAt,
            e.EndAt,
            e.RegistrationDeadline,
            e.Capacity,
            COALESCE(activeRegistrations.ActiveRegistrationCount, 0) AS ActiveRegistrationCount,
            e.Capacity - COALESCE(activeRegistrations.ActiveRegistrationCount, 0) AS AvailableSeats,
            CASE
                WHEN UTC_TIMESTAMP() < e.StartAt THEN 'Upcoming'
                WHEN UTC_TIMESTAMP() >= e.EndAt THEN 'Completed'
                ELSE 'Ongoing'
            END AS EventStatus,
            e.IsActive,
            e.CreatedAt,
            e.UpdatedAt
        FROM Events e
        INNER JOIN Categories c ON e.CategoryId = c.Id
        LEFT JOIN (
            SELECT EventId, COUNT(*) AS ActiveRegistrationCount
            FROM Registrations
            WHERE Status = 1
            GROUP BY EventId
        ) activeRegistrations ON activeRegistrations.EventId = e.Id
        """;
}
