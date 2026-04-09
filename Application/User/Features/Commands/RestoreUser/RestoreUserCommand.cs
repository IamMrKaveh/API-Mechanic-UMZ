namespace Application.User.Features.Commands.RestoreUser;

public record RestoreUserCommand(Guid Id) : IRequest<ServiceResult>;