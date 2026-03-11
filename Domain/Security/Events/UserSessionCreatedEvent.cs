namespace Domain.Security.Events;

public sealed record UserSessionCreatedEvent(
    UserSessionId SessionId,
    UserId UserId,
    DeviceInfo DeviceInfo,
    Common.ValueObjects.IpAddress IpAddress,
    DateTime ExpiresAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}