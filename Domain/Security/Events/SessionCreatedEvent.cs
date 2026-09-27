using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed record SessionCreatedEvent(
    SessionId SessionId,
    UserId UserId,
    DeviceInfo DeviceInfo,
    IpAddress IpAddress,
    DateTime ExpiresAt) : DomainEvent;
