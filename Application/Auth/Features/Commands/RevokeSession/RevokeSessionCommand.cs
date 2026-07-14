namespace Application.Auth.Features.Commands.RevokeSession;

public record RevokeSessionCommand(Guid UserId, Guid SessionId) : ICommand, IAuditableCommand
{
    public string AuditEventType => "SecurityEvent";

    public string AuditAction => "RevokeSession";

    public string? AuditEntityType => "Session";

    public string? AuditEntityId => SessionId.ToString();
}