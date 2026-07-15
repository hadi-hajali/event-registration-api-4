namespace EventRegistration.Api.Features.Dashboard;

public sealed record UpcomingEvent(
    ulong Id,
    string Title,
    string CategoryName,
    DateTime StartDate,
    string Location,
    int Capacity,
    long RegisteredCount);

public sealed record DashboardSummary(
    long TotalActiveCategories,
    long TotalActiveEvents,
    long TotalActiveParticipants,
    long TotalActiveRegistrations,
    IReadOnlyList<UpcomingEvent> UpcomingEvents);
