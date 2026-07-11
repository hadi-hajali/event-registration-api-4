namespace EventRegistration.Api.Features.Dashboard;

public sealed class DashboardSummaryResponse
{
    public int TotalActiveCategories { get; set; }

    public int TotalActiveParticipants { get; set; }

    public int TotalUpcomingEvents { get; set; }

    public int TotalActiveRegistrations { get; set; }

    public IReadOnlyList<UpcomingEventResponse> UpcomingEvents { get; set; }
        = new List<UpcomingEventResponse>();
}