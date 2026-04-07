using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.Security.Events;

public sealed class SessionExpiredEvent(
    UserSessionId sessionId,
    UserId userId) : DomainEvent
{
    public UserSessionId SessionId { get; } = sessionId;
    public UserId UserId { get; } = userId;
}