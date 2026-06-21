using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetCategories;

public record GetCategoriesQuery(
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page,
    int PageSize) : IPageQuery<CategoryListItemDto>;