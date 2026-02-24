using Application.Auth.Features.Shared;

namespace Application.Auth.Features.Commands.RefreshToken;

public record RefreshTokenCommand : IRequest<ServiceResult<AuthResult>>
{
    public required string RefreshToken { get; init; }
    public required string IpAddress { get; init; }
    public string? UserAgent { get; init; }
}