namespace Application.Auth.Features.Commands.LogoutAll;

public record LogoutAllCommand(int UserId) : IRequest<ServiceResult>;