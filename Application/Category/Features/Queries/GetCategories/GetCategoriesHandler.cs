namespace Application.Category.Features.Queries.GetCategories;

public class GetAdminCategoriesLegacyQueryHandler
    : IRequestHandler<GetAdminCategoriesLegacyQuery, ServiceResult<PaginatedResult<CategoryListItemDto>>>
{
    private readonly ICategoryQueryService _queryService;

    public GetAdminCategoriesLegacyQueryHandler(
        ICategoryQueryService queryService
        )
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<PaginatedResult<CategoryListItemDto>>> Handle(
        GetAdminCategoriesLegacyQuery request,
        CancellationToken ct
        )
    {
        var result = await _queryService.GetCategoriesPagedAsync(
            request.Search,
            isActive: null,
            includeDeleted: false,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<CategoryListItemDto>>.Success(result);
    }
}