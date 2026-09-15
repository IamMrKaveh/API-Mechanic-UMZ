using Application.Auth.Features.Shared;
using Microsoft.Extensions.Options;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Auth.Features.Commands.RefreshToken;

public class RefreshTokenHandler(
    IAuthService authService,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IOptions<JwtOptions> jwtOptions)
    : ICommandHandler<RefreshTokenCommand, AuthResult>
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    public async Task<ServiceResult<AuthResult>> Handle(
        RefreshTokenCommand request,
        CancellationToken ct)
    {
        var ipAddress = string.IsNullOrWhiteSpace(currentUserService.IpAddress)
            ? IpAddress.Unknown
            : IpAddress.Create(currentUserService.IpAddress);

        var result = await authService.RefreshTokenAsync(
            Domain.Security.ValueObjects.RefreshToken.Create(request.RefreshToken),
            ipAddress,
            currentUserService.UserAgent,
            ct);

        if (result.IsFailure)
            return ServiceResult<AuthResult>.Failure(result.Error);

        var (accessToken, refreshToken, user, isNewUser) = result.Value;

        return ServiceResult<AuthResult>.Success(new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.RefreshToken,
            AccessTokenExpiresAt = dateTimeProvider.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            User = user,
            IsNewUser = isNewUser
        });
    }
}