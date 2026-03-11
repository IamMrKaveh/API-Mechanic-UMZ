namespace Domain.Security.Events;

public sealed record UserSessionExpiredEvent(
    UserSessionId SessionId,
    UserId UserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}