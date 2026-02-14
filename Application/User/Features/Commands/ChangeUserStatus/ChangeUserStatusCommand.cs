namespace Application.User.Features.Commands.ChangeUserStatus;

public record ChangeUserStatusCommand(int Id, bool IsActive) : IRequest<ServiceResult>;