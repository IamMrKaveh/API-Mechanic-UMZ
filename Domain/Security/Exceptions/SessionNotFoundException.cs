using Domain.Security.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class SessionNotFoundException : DomainException
{
    public SessionId SessionId { get; }

    public override string ErrorCode => "SESSION_NOT_FOUND";

    public SessionNotFoundException(SessionId sessionId)
        : base($"نشست با شناسه '{sessionId}' یافت نشد.")
    {
        SessionId = sessionId;
    }
}