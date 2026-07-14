namespace Application.User.Features.Commands.RestoreUser;

public record RestoreUserCommand(
    Guid Id)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "AdminEvent";

    public string AuditAction => "UserRestored";

    public string? AuditEntityType => "User";

    public string? AuditEntityId => Id.ToString();
}