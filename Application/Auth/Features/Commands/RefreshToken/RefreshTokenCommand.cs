using Application.Auth.Features.Shared;
using Application.Common.Results;

namespace Application.Auth.Features.Commands.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken,
    string IpAddress,
    string? UserAgent) : IRequest<ServiceResult<AuthResult>>;