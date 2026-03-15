using Domain.Security.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class SessionNotFoundException(UserSessionId sessionId)
    : DomainException($"نشست با شناسه '{sessionId}' یافت نشد.")
{
    public UserSessionId SessionId { get; } = sessionId;
}