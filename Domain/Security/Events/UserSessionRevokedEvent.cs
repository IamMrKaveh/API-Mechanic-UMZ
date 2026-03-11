namespace Domain.Security.Events;

public sealed record UserSessionRevokedEvent(
    UserSessionId SessionId,
    UserId UserId,
    SessionRevocationReason Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}