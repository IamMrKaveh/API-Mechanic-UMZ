using Application.DTOs.Media;

namespace Application.Common.Interfaces.Admin.Media;

public interface IAdminMediaService
{
    Task<ServiceResult<PagedResultDto<MediaDto>>> GetAllMediaAsync(int page, int pageSize, string? entityType = null);
    Task<ServiceResult> DeleteMediaAsync(int id);
    Task<ServiceResult<(int Count, long Size)>> CleanupOrphanedMediaAsync();
}