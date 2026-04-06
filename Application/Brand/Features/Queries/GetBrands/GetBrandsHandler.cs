using Application.Brand.Contracts;
using Application.Brand.Features.Shared;
using Application.Common.Results;
using SharedKernel.Models;

namespace Application.Brand.Features.Queries.GetBrands;

public class GetBrandsHandler(
    IBrandQueryService brandQueryService) : IRequestHandler<GetBrandsQuery, ServiceResult<PaginatedResult<BrandListItemDto>>>
{
    private readonly IBrandQueryService _brandQueryService = brandQueryService;

    public async Task<ServiceResult<PaginatedResult<BrandListItemDto>>> Handle(
        GetBrandsQuery request,
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