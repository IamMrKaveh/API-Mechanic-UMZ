using Application.Auth.Features.Shared;

namespace Application.Auth.Features.Commands.RefreshToken;

public class RefreshTokenHandler(
    IAuthService authService,
    ICurrentUserService currentUserService)
    : ICommandHandler<RefreshTokenCommand, AuthResult>
{
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
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            User = user,
            IsNewUser = isNewUser
        });
    }
}