namespace Application.User.Features.Commands.UpdateUser;

public record UpdateUserCommand(
    Guid Id,
    string FirstName,
    string LastName)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "AdminEvent";

    public string AuditAction => "UpdateUser";

    public string? AuditEntityType => "User";

    public string? AuditEntityId => Id.ToString();
}