using Application.Common.Results;

namespace Application.Category.Features.Queries.GetPublicCategories;

public record GetPublicCategoriesQuery(
    string? Search,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<CategoryDto>>>;