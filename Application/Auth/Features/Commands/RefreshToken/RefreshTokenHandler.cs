using Application.Auth.Contracts;
using Application.Auth.Features.Shared;

namespace Application.Auth.Features.Commands.RefreshToken;

public class RefreshTokenHandler(IAuthService authService) : IRequestHandler<RefreshTokenCommand, ServiceResult<AuthResult>>
{
    public async Task<ServiceResult<AuthResult>> Handle(
        RefreshTokenCommand request,
        CancellationToken ct)
    {
        var result = await authService.RefreshTokenAsync(
            request.RefreshToken,
            request.IpAddress,
            request.UserAgent,
            ct);

        if (result.IsFailed || result.Value == default)
            return ServiceResult<AuthResult>.Failure(result.Error ?? "Refresh failed", result.StatusCode);

        var (accessToken, refreshTokenInfo, userDto, isNewUser) = result.Value;

        return ServiceResult<AuthResult>.Success(new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenInfo.FullToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(60),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30),
            User = userDto,
            IsNewUser = isNewUser
        });
    }
}