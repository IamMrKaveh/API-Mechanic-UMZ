namespace Application.Common.Interfaces;

public interface IUserService
{
    Task<ServiceResult<(string? Message, string? Otp)>> LoginAsync(LoginRequestDto request, string clientIp);
    Task<ServiceResult<(AuthResponseDto? Response, string? Error)>> VerifyOtpAsync(VerifyOtpRequestDto request, string clientIp, string userAgent);
    Task<ServiceResult<(object? Response, string? Error)>> RefreshTokenAsync(RefreshRequestDto request, string clientIp, string userAgent);
    Task<ServiceResult> LogoutAsync(string refreshToken);
    Task<ServiceResult<IEnumerable<UserProfileDto>>> GetUsersAsync(bool includeDeleted);
    Task<ServiceResult<UserProfileDto?>> GetUserByIdAsync(int id);
    Task<ServiceResult<UserProfileDto?>> GetUserProfileAsync(int userId);
    Task<ServiceResult<(UserProfileDto? User, string? Error)>> CreateUserAsync(Domain.User.User tUsers);
    Task<ServiceResult> UpdateUserAsync(int id, UpdateProfileDto updateRequest, int currentUserId, bool isAdmin);
    Task<ServiceResult> UpdateProfileAsync(int userId, UpdateProfileDto updateRequest);
    Task<ServiceResult> ChangeUserStatusAsync(int id, bool isActive);
    Task<ServiceResult> DeleteUserAsync(int id, int currentUserId);
    Task<ServiceResult> RestoreUserAsync(int id);
    Task<ServiceResult> DeleteAccountAsync(int userId);
}