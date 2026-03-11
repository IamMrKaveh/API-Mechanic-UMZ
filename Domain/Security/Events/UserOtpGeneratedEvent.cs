namespace Domain.Security.Events;

public sealed record UserOtpGeneratedEvent(
    UserOtpId OtpId,
    UserId UserId,
    OtpPurpose Purpose,
    DateTime ExpiresAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}