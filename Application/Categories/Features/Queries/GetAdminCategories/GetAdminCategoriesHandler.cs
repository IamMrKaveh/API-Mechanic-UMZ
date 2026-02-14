namespace Application.Categories.Features.Queries.GetAdminCategories;

public class GetAdminCategoriesHandler
    : IRequestHandler<GetAdminCategoriesQuery, ServiceResult<PaginatedResult<CategoryListItemDto>>>
{
    private readonly ICategoryQueryService _queryService;

    public GetAdminCategoriesHandler(ICategoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<PaginatedResult<CategoryListItemDto>>> Handle(
        GetAdminCategoriesQuery request, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetCategoriesPagedAsync(
            request.Search,
            request.IsActive,
            request.IncludeDeleted,
            request.Page,
            request.PageSize,
            cancellationToken);

        return ServiceResult<PaginatedResult<CategoryListItemDto>>.Success(result);
    }
}