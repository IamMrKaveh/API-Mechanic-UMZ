using Application.Auth.Features.Shared;
using Microsoft.Extensions.Options;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Auth.Features.Commands.RefreshToken;

public class RefreshTokenHandler(
    IAuthService authService,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTimeProvider,
    IOptions<JwtOptions> jwtOptions,
    IOptions<AuthOptions> authOptions)
    : ICommandHandler<RefreshTokenCommand, AuthResult>
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly AuthOptions _authOptions = authOptions.Value;

    public async Task<ServiceResult<AuthResult>> Handle(
        RefreshTokenCommand request,
        CancellationToken ct)
    {
        var refreshToken = Domain.Security.ValueObjects.RefreshToken.Create(request.RefreshToken);
        var ipAddress = IpAddress.Create(currentUser.IpAddress ?? IpAddress.Unknown.Value);

        var result = await authService.RefreshTokenAsync(
            refreshToken,
            ipAddress,
            currentUser.UserAgent,
            ct);

        if (result.IsFailed || result.Value == default)
            return ServiceResult<AuthResult>.Failure(result.Error ?? "Refresh failed", result.Type);

        var (accessToken, refreshTokenInfo, userDto, isNewUser) = result.Value;

        var now = dateTimeProvider.UtcNow;

        return ServiceResult<AuthResult>.Success(new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenInfo.RefreshToken,
            AccessTokenExpiresAt = now.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
            RefreshTokenExpiresAt = now.AddDays(_authOptions.SessionExpirationDays),
            User = userDto,
            IsNewUser = isNewUser
        });
    }
}