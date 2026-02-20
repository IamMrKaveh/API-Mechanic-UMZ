namespace Application.Brand.Features.Queries.GetAdminBrands;

public class GetAdminBrandsHandler
    : IRequestHandler<GetAdminBrandsQuery, ServiceResult<PaginatedResult<BrandListItemDto>>>
{
    private readonly ICategoryQueryService _queryService;

    public GetAdminBrandsHandler(ICategoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<PaginatedResult<BrandListItemDto>>> Handle(
        GetAdminBrandsQuery request, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetBrandsPagedAsync(
            request.CategoryId,
            request.Search,
            request.IsActive,
            request.IncludeDeleted,
            request.Page,
            request.PageSize,
            cancellationToken);

        return ServiceResult<PaginatedResult<BrandListItemDto>>.Success(result);
    }
}