using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed class SessionCreatedEvent(
    SessionId sessionId,
    UserId userId,
    DeviceInfo deviceInfo,
    IpAddress ipAddress,
    DateTime expiresAt) : DomainEvent
{
    public SessionId SessionId { get; } = sessionId;
    public UserId UserId { get; } = userId;
    public DeviceInfo DeviceInfo { get; } = deviceInfo;
    public IpAddress IpAddress { get; } = ipAddress;
    public DateTime ExpiresAt { get; } = expiresAt;
}