namespace Application.User.Features.Commands.RestoreUser;

public record RestoreUserCommand(int Id) : IRequest<ServiceResult>;