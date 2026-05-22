using Domain.Security.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class SessionExpiredException(SessionId sessionId) : DomainException($"نشست '{sessionId}' منقضی شده است.")
{
    public SessionId SessionId { get; } = sessionId;

    public override string ErrorCode => "SESSION_EXPIRED";
}