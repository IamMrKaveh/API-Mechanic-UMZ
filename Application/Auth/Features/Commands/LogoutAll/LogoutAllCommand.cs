namespace Application.Auth.Features.Commands.LogoutAll;

public record LogoutAllCommand(Guid? TargetUserId = null) : ICommand, IAuditableCommand
{
    public string AuditEventType => "SecurityEvent";
    public string AuditAction => "LogoutAll";
    public string? AuditEntityType => "User";
    public string? AuditEntityId => TargetUserId?.ToString() ?? string.Empty;
}