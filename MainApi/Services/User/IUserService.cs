namespace MainApi.Services.User;

public interface IUserService
{
    Task<IEnumerable<UserProfileDto>> GetUsersAsync(bool includeDeleted);
    Task<UserProfileDto?> GetUserByIdAsync(int id);
    Task<UserProfileDto?> GetUserProfileAsync(int userId);
    Task<(bool Success, UserProfileDto? User, string? Error)> CreateUserAsync(TUsers tUsers);
    Task<(bool Success, string? Error)> UpdateUserAsync(int id, UpdateProfileDto updateRequest, int currentUserId, bool isAdmin);
    Task<(bool Success, string? Error)> UpdateProfileAsync(int userId, UpdateProfileDto updateRequest);
    Task<(bool Success, string? Error)> ChangeUserStatusAsync(int id, bool isActive);
    Task<(bool Success, string? Error)> DeleteUserAsync(int id, int currentUserId);
    Task<(bool Success, string? Error)> RestoreUserAsync(int id);
    Task<(bool Success, string? Error)> DeleteAccountAsync(int userId);
    Task<(bool Success, string? Message, string? Otp)> LoginAsync(LoginRequestDto request, string clientIp);
    Task<(AuthResponseDto? Response, string? Error)> VerifyOtpAsync(VerifyOtpRequestDto request, string clientIp, string userAgent);
    Task<(object? Response, string? Error)> RefreshTokenAsync(RefreshRequestDto request, string clientIp, string userAgent);
    Task<(bool Success, string? Error)> LogoutAsync(string refreshToken);
    Task<IEnumerable<ProductReviewDto>> GetUserReviewsAsync(int userId);
}