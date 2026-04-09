using Application.Common.Results;

namespace Application.Auth.Features.Commands.Logout;

public record LogoutCommand(
    Guid UserId,
    string? RefreshToken) : IRequest<ServiceResult>;