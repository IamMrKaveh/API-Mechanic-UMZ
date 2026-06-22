namespace Application.User.Features.Commands.ChangeUserRole;

public record ChangeUserRoleCommand(
    Guid UserId,
    bool IsAdmin)
    : ICommand;