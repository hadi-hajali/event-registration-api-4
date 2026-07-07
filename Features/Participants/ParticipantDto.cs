namespace EventRegistration.Api.Features.Participants;

public sealed record ParticipantDto(
    ulong Id,
    string FullName,
    string Email,
    string Phone,
    DateTime? DateOfBirth,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);