namespace EventRegistration.Api.Features.Events;

internal static class EventSql
{
    public const string SelectById = """
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
        WHERE e.Id = @Id
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
            e.UpdatedAt;
        """;
}
