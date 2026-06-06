namespace Application.Auth.Features.Commands.Logout;

public record LogoutCommand(string? RefreshToken) : IRequest<ServiceResult>;