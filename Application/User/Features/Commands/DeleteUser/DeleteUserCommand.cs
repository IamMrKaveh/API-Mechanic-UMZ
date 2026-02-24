namespace Application.User.Features.Commands.DeleteUser;

public record DeleteUserCommand(int Id, int CurrentUserId) : IRequest<ServiceResult>;