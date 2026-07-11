namespace EventRegistration.Api.Features.Dashboard;

public sealed class UpcomingEventResponse
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public string Location { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public int ActiveRegistrationCount { get; set; }

    public int AvailableSeats { get; set; }
}