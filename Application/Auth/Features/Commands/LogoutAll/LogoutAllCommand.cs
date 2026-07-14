namespace Application.Auth.Features.Commands.LogoutAll;

public record LogoutAllCommand(Guid UserId) : ICommand, IAuditableCommand
{
    public string AuditEventType => "SecurityEvent";

    public string AuditAction => "LogoutAll";

    public string? AuditEntityType => "User";

    public string? AuditEntityId => UserId.ToString();
}