namespace EventRegistration.Api.Features.Events;

public sealed class EventResponse
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Location { get; set; } = string.Empty;
    public ulong CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long RegisteredCount { get; set; }
}

public sealed record EventRequest(
    string Name,
    string? Description,
    string Location,
    ulong CategoryId,
    int Capacity,
    DateTime StartAt,
    DateTime EndAt,
    DateTime RegistrationDeadline,
    bool IsActive);

public sealed record PagedEventResponse(
    IReadOnlyList<EventResponse> Items,
    int Page,
    int PageSize,
    long TotalCount,
    int TotalPages);
