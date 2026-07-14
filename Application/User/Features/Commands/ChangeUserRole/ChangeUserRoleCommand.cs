namespace Application.User.Features.Commands.ChangeUserRole;

public record ChangeUserRoleCommand(
    Guid UserId,
    bool IsAdmin)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "AdminEvent";

    public string AuditAction => "RoleChanged";

    public string? AuditEntityType => "User";

    public string? AuditEntityId => UserId.ToString();
}