using Application.Auth.Features.Shared;

namespace Application.Auth.Features.Commands.RefreshToken;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, ServiceResult<AuthResult>>
{
    private readonly IAuthService _authService;

    public RefreshTokenHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<ServiceResult<AuthResult>> Handle(
        RefreshTokenCommand request,
        CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(
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