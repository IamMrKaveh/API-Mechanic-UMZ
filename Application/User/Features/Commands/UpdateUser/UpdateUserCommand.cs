namespace Application.User.Features.Commands.UpdateUser;

public record UpdateUserCommand(int Id, UpdateProfileDto UpdateRequest, int CurrentUserId) : IRequest<ServiceResult>;