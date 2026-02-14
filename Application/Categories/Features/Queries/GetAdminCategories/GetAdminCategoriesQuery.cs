namespace Application.Categories.Features.Queries.GetAdminCategories;

public record GetAdminCategoriesQuery(
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<CategoryListItemDto>>>;