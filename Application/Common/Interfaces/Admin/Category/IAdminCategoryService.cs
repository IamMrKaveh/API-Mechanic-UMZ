using Application.DTOs.Category;

namespace Application.Common.Interfaces.Admin.Category;

public interface IAdminCategoryService
{
    Task<ServiceResult<PagedResultDto<CategoryViewDto>>> GetCategoriesAsync(string? search, int page, int pageSize);
    Task<ServiceResult<CategoryDetailViewDto?>> GetCategoryByIdAsync(int id, int page, int pageSize);
    Task<ServiceResult<CategoryViewDto>> CreateCategoryAsync(CategoryCreateDto dto);
    Task<ServiceResult> UpdateCategoryAsync(int id, CategoryUpdateDto dto);
    Task<ServiceResult> DeleteCategoryAsync(int id);
}