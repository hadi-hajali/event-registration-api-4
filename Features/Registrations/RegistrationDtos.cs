namespace EventRegistration.Api.Features.Registrations;

public sealed class RegistrationResponse
{
    public ulong Id { get; set; }
    public ulong EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public ulong ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public string ParticipantPhone { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}

public sealed record CreateRegistrationRequest(ulong ParticipantId, string? Notes);

public sealed record PagedRegistrationResponse(
    IReadOnlyList<RegistrationResponse> Items,
    int Page,
    int PageSize,
    long TotalCount,
    int TotalPages);
