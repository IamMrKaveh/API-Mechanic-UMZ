using Application.Common.Results;

namespace Application.User.Features.Commands.ChangePassword;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : IRequest<ServiceResult>;