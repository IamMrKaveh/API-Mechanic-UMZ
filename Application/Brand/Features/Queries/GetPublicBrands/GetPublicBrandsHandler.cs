using Application.Brand.Contracts;
using Application.Brand.Features.Shared;

namespace Application.Brand.Features.Queries.GetPublicBrands;

public sealed class GetPublicBrandsHandler(IBrandQueryService brandQueryService) : IRequestHandler<GetPublicBrandsQuery, ServiceResult<IReadOnlyList<BrandDto>>>
{
    private readonly IBrandQueryService _brandQueryService = brandQueryService;

    public async Task<ServiceResult<IReadOnlyList<BrandDto>>> Handle(
        GetPublicBrandsQuery request,
        CancellationToken ct)
    {
        var brands = await _brandQueryService.GetPublicBrandsAsync(request.CategoryId, ct);
        return ServiceResult<IReadOnlyList<BrandDto>>.Success(brands);
    }
}