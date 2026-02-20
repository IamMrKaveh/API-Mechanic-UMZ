namespace Application.Brand.Features.Queries.GetBrands;

public class GetAdminBrandsLegacyQueryHandler
    : IRequestHandler<GetAdminBrandsLegacyQuery, ServiceResult<PaginatedResult<BrandListItemDto>>>
{
    private readonly ICategoryQueryService _queryService;

    public GetAdminBrandsLegacyQueryHandler(ICategoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<PaginatedResult<BrandListItemDto>>> Handle(
        GetAdminBrandsLegacyQuery request, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetBrandsPagedAsync(
            request.CategoryId,
            request.Search,
            isActive: null,
            includeDeleted: false,
            request.Page,
            request.PageSize,
            cancellationToken);

        return ServiceResult<PaginatedResult<BrandListItemDto>>.Success(result);
    }
}