using Application.DTOs.Category;

namespace Application.Common.Interfaces.Admin.Category;

public interface IAdminCategoryGroupService
{
    Task<ServiceResult<PagedResultDto<CategoryGroupViewDto>>> GetPagedAsync(int? categoryId, string? search, int page, int pageSize);
    Task<ServiceResult<CategoryGroupViewDto?>> GetByIdAsync(int id);
    Task<ServiceResult<CategoryGroupViewDto>> CreateAsync(CategoryGroupCreateDto dto);
    Task<ServiceResult> UpdateAsync(int id, CategoryGroupUpdateDto dto);
    Task<ServiceResult> DeleteAsync(int id);
}