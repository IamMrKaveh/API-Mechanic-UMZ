using Application.Categories.Features.Shared;

namespace Application.Categories.Features.Queries.GetAdminCategoryGroups;

public record GetAdminCategoryGroupsQuery(
    int? CategoryId,
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<CategoryGroupListItemDto>>>;