using Application.Auth.Features.Shared;

namespace Application.Auth.Features.Commands.RefreshToken;

public class RefreshTokenHandler(IAuthService authService) : IRequestHandler<RefreshTokenCommand, ServiceResult<AuthResult>>
{
    public async Task<ServiceResult<AuthResult>> Handle(
        RefreshTokenCommand request,
        CancellationToken ct)
    {
        var refreshToken = Domain.Security.ValueObjects.RefreshToken.Create(request.RefreshToken);
        var ipAddress = IpAddress.Create(request.IpAddress);

        var result = await authService.RefreshTokenAsync(
            refreshToken,
            ipAddress,
            request.UserAgent,
            ct);

        if (result.IsFailed || result.Value == default)
            return ServiceResult<AuthResult>.Failure(result.Error ?? "Refresh failed", result.Type);

        var (accessToken, refreshTokenInfo, userDto, isNewUser) = result.Value;

        return ServiceResult<AuthResult>.Success(new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenInfo.RefreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(60),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30),
            User = userDto,
            IsNewUser = isNewUser
        });
    }
}