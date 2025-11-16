namespace Application.Common.Interfaces;

public interface IAdminUserService
{
    Task<ServiceResult<IEnumerable<UserProfileDto>>> GetUsersAsync(bool includeDeleted);
    Task<ServiceResult<UserProfileDto?>> GetUserByIdAsync(int id);
    Task<ServiceResult<(UserProfileDto? User, string? Error)>> CreateUserAsync(Domain.User.User tUsers);
    Task<ServiceResult> UpdateUserAsync(int id, UpdateProfileDto updateRequest, int currentUserId);
    Task<ServiceResult> ChangeUserStatusAsync(int id, bool isActive);
    Task<ServiceResult> DeleteUserAsync(int id, int currentUserId);
    Task<ServiceResult> RestoreUserAsync(int id);
}