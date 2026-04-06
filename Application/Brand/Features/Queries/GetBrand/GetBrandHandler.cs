using Application.Brand.Contracts;
using Application.Brand.Features.Shared;
using Application.Common.Results;

namespace Application.Brand.Features.Queries.GetBrand;

public class GetBrandHandler(
    IBrandQueryService brandQueryService) : IRequestHandler<GetBrandQuery, ServiceResult<BrandDetailDto>>
{
    private readonly IBrandQueryService _brandQueryService = brandQueryService;

    public async Task<ServiceResult<BrandDetailDto>> Handle(
        GetBrandQuery request,
        CancellationToken ct)
    {
        var brand = await _brandQueryService.GetBrandDetailAsync(request.Id, ct);
        return brand is null
            ? ServiceResult<BrandDetailDto>.NotFound("برند یافت نشد.")
            : ServiceResult<BrandDetailDto>.Success(brand);
    }
}