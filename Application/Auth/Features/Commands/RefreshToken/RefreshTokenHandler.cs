using Application.Auth.Features.Shared;

namespace Application.Auth.Features.Commands.RefreshToken;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, ServiceResult<AuthResult>>
{
    private readonly IAuthService _authService;
    private readonly IMapper _mapper;

    public RefreshTokenHandler(
        IAuthService authService,
        IMapper mapper
        )
    {
        _authService = authService;
        _mapper = mapper;
    }

    public async Task<ServiceResult<AuthResult>> Handle(
        RefreshTokenCommand request,
        CancellationToken ct
        )
    {
        var result = await _authService.RefreshTokenAsync(
            request.RefreshToken,
            request.IpAddress,
            request.UserAgent,
            ct);

        if (result.IsFailed || result.Data == default)
        {
            return ServiceResult<AuthResult>.Failure(result.Error ?? "Refresh failed", result.StatusCode);
        }

        var (accessToken, refreshTokenInfo, user, isNewUser) = result.Data;

        return ServiceResult<AuthResult>.Success(new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenInfo.FullToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(60),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30),
            User = _mapper.Map<UserProfileDto>(user),
            IsNewUser = isNewUser
        });
    }
}