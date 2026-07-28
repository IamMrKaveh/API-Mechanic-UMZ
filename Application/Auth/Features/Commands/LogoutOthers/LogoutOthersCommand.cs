namespace Application.Auth.Features.Commands.LogoutOthers;

public record LogoutOthersCommand : ICommand, IAuditableCommand
{
    public string AuditEventType => "SecurityEvent";
    public string AuditAction => "LogoutOthers";
    public string? AuditEntityType => "User";
    public string? AuditEntityId => string.Empty;
}
