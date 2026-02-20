using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetCategories;

public record GetAdminCategoriesLegacyQuery(string? Search, int Page, int PageSize)
    : IRequest<ServiceResult<PaginatedResult<CategoryListItemDto>>>;