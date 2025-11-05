namespace MainApi.Services.Category;

public interface ICategoryService
{
    Task<(IEnumerable<object> Categories, int TotalItems)> GetCategoriesAsync(string? search, int page, int pageSize);
    Task<object?> GetCategoryByIdAsync(int id, int page, int pageSize);
    Task<object> CreateCategoryAsync(CategoryDto categoryDto);
    Task<(bool Success, string? ErrorMessage)> UpdateCategoryAsync(int id, CategoryDto categoryDto);
    Task<(bool Success, string? ErrorMessage)> DeleteCategoryAsync(int id);
}