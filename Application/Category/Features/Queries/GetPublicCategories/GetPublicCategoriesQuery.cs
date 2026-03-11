using Application.Common.Models;

namespace Application.Category.Features.Queries.GetPublicCategories;

public record GetPublicCategoriesQuery(
    string? Search,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<CategoryDto>>>;