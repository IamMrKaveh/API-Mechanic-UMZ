using Application.DTOs.Category;

namespace Application.Common.Interfaces.Category;

public interface ICategoryService
{
    Task<ServiceResult<IEnumerable<CategoryHierarchyDto>>> GetCategoryHierarchyAsync();
    Task<ServiceResult<PagedResultDto<CategoryViewDto>>> GetCategoriesAsync(string? search, int page, int pageSize);
    Task<ServiceResult<CategoryDetailViewDto?>> GetCategoryByIdAsync(int id, int page, int pageSize);
}