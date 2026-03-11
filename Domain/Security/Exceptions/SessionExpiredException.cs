namespace Domain.Security.Exceptions;

public sealed class SessionExpiredException(UserSessionId sessionId)
    : DomainException($"نشست '{sessionId}' منقضی شده است.")
{
    public UserSessionId SessionId { get; } = sessionId;
}