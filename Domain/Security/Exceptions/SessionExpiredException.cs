using Domain.Security.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class SessionExpiredException : DomainException
{
    public SessionId SessionId { get; }

    public override string ErrorCode => "SESSION_EXPIRED";

    public SessionExpiredException(SessionId sessionId)
        : base($"نشست '{sessionId}' منقضی شده است.")
    {
        SessionId = sessionId;
    }
}