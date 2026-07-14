namespace Application.Auth.Features.Commands.AdminRevokeSession;

public record AdminRevokeSessionCommand(Guid TargetUserId, Guid SessionId) : ICommand, IAuditableCommand
{
    public string AuditEventType => "SecurityEvent";

    public string AuditAction => "AdminRevokeSession";

    public string? AuditEntityType => "Session";

    public string? AuditEntityId => SessionId.ToString();
}