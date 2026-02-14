namespace Application.Auth.Contracts;

public interface IAuthService
{
    Task<ServiceResult> RequestOtpAsync(string phoneNumber, string ipAddress, CancellationToken ct = default);

    Task<ServiceResult<Application.Auth.Features.Shared.AuthResult>> VerifyOtpAsync(
        string phoneNumber, string code, string ipAddress, string? userAgent, CancellationToken ct = default);

    Task<ServiceResult<Application.Auth.Features.Shared.AuthResult>> RefreshTokenAsync(
        string refreshToken, string ipAddress, string? userAgent, CancellationToken ct = default);

    Task<ServiceResult> LogoutAsync(int userId, string refreshToken, CancellationToken ct = default);

    Task<ServiceResult> LogoutAllAsync(int userId, CancellationToken ct = default);
}