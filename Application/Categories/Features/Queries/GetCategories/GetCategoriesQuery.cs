namespace Application.Categories.Features.Queries.GetCategories;

public record GetAdminCategoriesLegacyQuery(string? Search, int Page, int PageSize)
    : IRequest<ServiceResult<PaginatedResult<CategoryListItemDto>>>;