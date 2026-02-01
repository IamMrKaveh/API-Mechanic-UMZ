using Application.DTOs.User;

namespace Application.Common.Interfaces.Admin.User;

public interface IAdminUserService
{
    Task<ServiceResult<PagedResultDto<UserProfileDto>>> GetUsersAsync(bool includeDeleted, int page, int pageSize);
    Task<ServiceResult<UserProfileDto?>> GetUserByIdAsync(int id);
    Task<ServiceResult<(UserProfileDto? User, string? Error)>> CreateUserAsync(User tUsers);
    Task<ServiceResult> UpdateUserAsync(int id, UpdateProfileDto updateRequest, int currentUserId);
    Task<ServiceResult> ChangeUserStatusAsync(int id, bool isActive);
    Task<ServiceResult> DeleteUserAsync(int id, int currentUserId);
    Task<ServiceResult> RestoreUserAsync(int id);
}