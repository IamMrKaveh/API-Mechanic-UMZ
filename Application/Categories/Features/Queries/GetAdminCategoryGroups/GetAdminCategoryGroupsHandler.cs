namespace Application.Categories.Features.Queries.GetAdminCategoryGroups;

public class GetAdminCategoryGroupsHandler
    : IRequestHandler<GetAdminCategoryGroupsQuery, ServiceResult<PaginatedResult<CategoryGroupListItemDto>>>
{
    private readonly ICategoryQueryService _queryService;

    public GetAdminCategoryGroupsHandler(ICategoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<PaginatedResult<CategoryGroupListItemDto>>> Handle(
        GetAdminCategoryGroupsQuery request, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetCategoryGroupsPagedAsync(
            request.CategoryId,
            request.Search,
            request.IsActive,
            request.IncludeDeleted,
            request.Page,
            request.PageSize,
            cancellationToken);

        return ServiceResult<PaginatedResult<CategoryGroupListItemDto>>.Success(result);
    }
}