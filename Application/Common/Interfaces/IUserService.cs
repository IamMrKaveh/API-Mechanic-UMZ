namespace Application.Common.Interfaces;

public interface IUserService
{
    Task<ServiceResult<(string? Message, string? Otp)>> LoginAsync(LoginRequestDto request, string clientIp);
    Task<ServiceResult<(AuthResponseDto? Response, string? Error)>> VerifyOtpAsync(VerifyOtpRequestDto request, string clientIp, string userAgent);
    Task<ServiceResult<(object? Response, string? Error)>> RefreshTokenAsync(RefreshRequestDto request, string clientIp, string userAgent);
    Task<ServiceResult> LogoutAsync(string refreshToken);
    Task<ServiceResult<UserProfileDto?>> GetUserByIdAsync(int id);
    Task<ServiceResult<UserProfileDto?>> GetUserProfileAsync(int userId);
    Task<ServiceResult> UpdateUserAsync(int id, UpdateProfileDto updateRequest, int currentUserId, bool isAdmin);
    Task<ServiceResult> UpdateProfileAsync(int userId, UpdateProfileDto updateRequest);
    Task<ServiceResult> DeleteAccountAsync(int userId);
    Task<ServiceResult<UserAddressDto?>> AddUserAddressAsync(int userId, CreateUserAddressDto addressDto);
    Task<ServiceResult<UserAddressDto?>> UpdateUserAddressAsync(int userId, int addressId, UpdateUserAddressDto addressDto);
    Task<ServiceResult> DeleteUserAddressAsync(int userId, int addressId);
}