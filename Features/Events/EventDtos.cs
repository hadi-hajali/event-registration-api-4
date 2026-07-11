namespace EventRegistration.Api.Features.Events;

public sealed record EventListQueryParameters(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    ulong? CategoryId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    bool? IsActive = null);

public sealed class EventListItem
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ulong CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int Capacity { get; set; }
    public int ActiveRegistrationCount { get; set; }
    public int AvailableSeats { get; set; }
    public string EventStatus { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class EventDetailsResponse
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ulong CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public int Capacity { get; set; }
    public int ActiveRegistrationCount { get; set; }
    public int AvailableSeats { get; set; }
    public string EventStatus { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed record CreateEventRequest(
    ulong CategoryId,
    string Name,
    string? Description,
    string Location,
    DateTime StartAt,
    DateTime EndAt,
    DateTime RegistrationDeadline,
    int Capacity,
    bool IsActive = true);

public sealed record UpdateEventRequest(
    ulong CategoryId,
    string Name,
    string? Description,
    string Location,
    DateTime StartAt,
    DateTime EndAt,
    DateTime RegistrationDeadline,
    int Capacity,
    bool IsActive);
