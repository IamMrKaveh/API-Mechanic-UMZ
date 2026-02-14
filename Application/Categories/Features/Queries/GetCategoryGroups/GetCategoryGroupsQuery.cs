namespace Application.Categories.Features.Queries.GetCategoryGroups;

public record GetAdminCategoryGroupsLegacyQuery(int? CategoryId, string? Search, int Page, int PageSize)
    : IRequest<ServiceResult<PaginatedResult<CategoryGroupListItemDto>>>;