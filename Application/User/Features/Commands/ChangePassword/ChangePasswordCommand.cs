namespace Application.User.Features.Commands.ChangePassword;

public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword)
    : ICommand;