using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed record SessionCreatedEvent(
    UserSessionId SessionId,
    UserId UserId,
    DeviceInfo DeviceInfo,
    IpAddress IpAddress,
    DateTime ExpiresAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}