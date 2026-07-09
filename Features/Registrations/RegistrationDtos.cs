namespace EventRegistration.Api.Features.Registrations;

public sealed class RegistrationResponse
{
    public ulong Id { get; set; }
    public ulong EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public ulong ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public int Status { get; set; }
    public string? Notes { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}

public sealed record CreateRegistrationRequest(ulong EventId, ulong ParticipantId, string? Notes);

public sealed record RegistrationListItem(ulong Id, ulong EventId, string EventName, ulong ParticipantId, string ParticipantName, string ParticipantEmail, int Status, string? Notes, DateTime RegisteredAt, DateTime? CancelledAt);
