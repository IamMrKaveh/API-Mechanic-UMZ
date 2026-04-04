using Application.Common.Results;

namespace Application.Brand.Features.Queries.GetBrands;

public class GetAdminBrandsLegacyQueryHandler
    : IRequestHandler<GetAdminBrandsLegacyQuery, ServiceResult<PaginatedResult<BrandListItemDto>>>
{
    private readonly IBrandQueryService _brandQueryService;

    public GetAdminBrandsLegacyQueryHandler(IBrandQueryService brandQueryService)
    {
        _brandQueryService = brandQueryService;
    }

    public async Task<ServiceResult<PaginatedResult<BrandListItemDto>>> Handle(
        GetAdminBrandsLegacyQuery request,
        CancellationToken ct)
    {
        var result = await _brandQueryService.GetBrandsPagedAsync(
            request.CategoryId,
            request.Search,
            isActive: null,
            includeDeleted: false,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<BrandListItemDto>>.Success(result);
    }
}