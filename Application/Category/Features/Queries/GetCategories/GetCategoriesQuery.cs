using Application.Category.Features.Shared;
using Application.Common.Results;
using SharedKernel.Models;

namespace Application.Category.Features.Queries.GetCategories;

public record GetCategoriesQuery(
    int? ParentId,
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<CategoryListItemDto>>>;