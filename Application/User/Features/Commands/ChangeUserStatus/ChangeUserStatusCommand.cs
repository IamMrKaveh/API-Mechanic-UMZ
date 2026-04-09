namespace Application.User.Features.Commands.ChangeUserStatus;

public record ChangeUserStatusCommand(Guid UserId, bool IsActive) : IRequest<ServiceResult>;