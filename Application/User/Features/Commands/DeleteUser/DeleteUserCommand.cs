namespace Application.User.Features.Commands.DeleteUser;

public record DeleteUserCommand(
    Guid Id)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "AdminEvent";

    public string AuditAction => "DeleteUser";

    public string? AuditEntityType => "User";

    public string? AuditEntityId => Id.ToString();
}