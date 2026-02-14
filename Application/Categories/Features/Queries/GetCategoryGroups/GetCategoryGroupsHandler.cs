namespace Application.Categories.Features.Queries.GetCategoryGroups;

public class GetAdminCategoryGroupsLegacyQueryHandler
    : IRequestHandler<GetAdminCategoryGroupsLegacyQuery, ServiceResult<PaginatedResult<CategoryGroupListItemDto>>>
{
    private readonly ICategoryQueryService _queryService;

    public GetAdminCategoryGroupsLegacyQueryHandler(ICategoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<PaginatedResult<CategoryGroupListItemDto>>> Handle(
        GetAdminCategoryGroupsLegacyQuery request, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetCategoryGroupsPagedAsync(
            request.CategoryId,
            request.Search,
            isActive: null,
            includeDeleted: false,
            request.Page,
            request.PageSize,
            cancellationToken);

        return ServiceResult<PaginatedResult<CategoryGroupListItemDto>>.Success(result);
    }
}