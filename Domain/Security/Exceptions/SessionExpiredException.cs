using Domain.Common.Exceptions;
using Domain.Security.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class SessionExpiredException : DomainException
{
    public UserSessionId SessionId { get; }

    public override string ErrorCode => "SESSION_EXPIRED";

    public SessionExpiredException(UserSessionId sessionId)
        : base($"نشست '{sessionId}' منقضی شده است.")
    {
        SessionId = sessionId;
    }
}