namespace Application.Common.Interfaces;

public interface ICategoryGroupService
{
    Task<ServiceResult<PagedResultDto<CategoryGroupViewDto>>> GetPagedAsync(int? categoryId, string? search, int page, int pageSize);
    Task<ServiceResult<CategoryGroupViewDto?>> GetByIdAsync(int id);
}