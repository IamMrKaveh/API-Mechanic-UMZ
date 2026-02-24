namespace Application.User.Features.Commands.ChangePassword;

public record ChangePasswordCommand(int UserId, ChangePasswordDto Dto) : IRequest<ServiceResult>;