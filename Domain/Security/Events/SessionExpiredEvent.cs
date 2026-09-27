using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed record SessionExpiredEvent(
    SessionId SessionId,
    UserId UserId) : DomainEvent;
