namespace Application.Auth.Features.Commands.LogoutAll;

public record LogoutAllCommand(Guid UserId) : IRequest<ServiceResult>;