using Domain.Security.ValueObjects;
using SharedKernel.Exceptions;

namespace Domain.Security.Exceptions;

public sealed class SessionExpiredException(SessionId sessionId)
    : SingleValueDomainException<SessionId>(
        "SESSION_EXPIRED",
        sessionId,
        $"نشست '{sessionId}' منقضی شده است.")
{
    public SessionId SessionId => Value;
}
