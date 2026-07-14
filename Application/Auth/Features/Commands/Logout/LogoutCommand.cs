namespace Application.Auth.Features.Commands.Logout;

public record LogoutCommand(string? RefreshToken) : ICommand, IAuditableCommand
{
    public string AuditEventType => "SecurityEvent";

    public string AuditAction => "Logout";

    public string? AuditEntityType => "Session";

    public string? AuditEntityId => null;
}