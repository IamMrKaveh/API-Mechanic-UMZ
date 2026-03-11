namespace Domain.Security.Events;

public sealed record UserOtpVerifiedEvent(
    UserOtpId OtpId,
    UserId UserId,
    OtpPurpose Purpose) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}