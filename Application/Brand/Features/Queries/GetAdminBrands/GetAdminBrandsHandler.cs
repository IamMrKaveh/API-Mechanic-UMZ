using Application.Brand.Contracts;
using Application.Common.Models;

namespace Application.Brand.Features.Queries.GetAdminBrands;

public class GetAdminBrandsHandler
    : IRequestHandler<GetAdminBrandsQuery, ServiceResult<PaginatedResult<BrandListItemDto>>>
{
    private readonly IBrandQueryService _brandQueryService;

    public GetAdminBrandsHandler(IBrandQueryService brandQueryService)
    {
        _brandQueryService = brandQueryService;
    }

    public async Task<ServiceResult<PaginatedResult<BrandListItemDto>>> Handle(
        GetAdminBrandsQuery request,
        CancellationToken ct)
    {
        var result = await _brandQueryService.GetBrandsPagedAsync(
            request.CategoryId,
            request.Search,
            request.IsActive,
            request.IncludeDeleted,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<BrandListItemDto>>.Success(result);
    }
}