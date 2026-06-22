namespace Application.User.Features.Commands.DeleteUser;

public record DeleteUserCommand(
    Guid Id)
    : ICommand;