using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetCategories;

public record GetCategoriesQuery(
    string? Search,
    bool? IsActive,
    bool IncludeDeleted) : IRequest<ServiceResult<PaginatedResult<CategoryListItemDto>>>;