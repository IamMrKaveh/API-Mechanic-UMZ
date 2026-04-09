using Application.Brand.Contracts;
using Application.Brand.Features.Shared;
using Domain.Brand.ValueObjects;

namespace Application.Brand.Features.Queries.GetBrand;

public class GetBrandHandler(
    IBrandQueryService brandQueryService) : IRequestHandler<GetBrandQuery, ServiceResult<BrandDetailDto>>
{
    private readonly IBrandQueryService _brandQueryService = brandQueryService;

    public async Task<ServiceResult<BrandDetailDto>> Handle(GetBrandQuery request, CancellationToken ct)
    {
        var brandId = BrandId.From(request.Id);
        var brand = await _brandQueryService.GetBrandDetailAsync(brandId, ct);
        return brand is null
            ? ServiceResult<BrandDetailDto>.NotFound("برند یافت نشد.")
            : ServiceResult<BrandDetailDto>.Success(brand);
    }
}