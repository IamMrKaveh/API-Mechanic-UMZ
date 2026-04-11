using Application.Brand.Features.Shared;
using Domain.Brand.ValueObjects;

namespace Application.Brand.Features.Queries.GetBrandDetail;

public class GetBrandDetailHandler(IBrandQueryService brandQueryService) : IRequestHandler<GetBrandDetailQuery, ServiceResult<BrandDetailDto?>>
{
    public async Task<ServiceResult<BrandDetailDto?>> Handle(GetBrandDetailQuery request, CancellationToken ct)
    {
        var brandId = BrandId.From(request.BrandId);
        var result = await brandQueryService.GetBrandDetailAsync(brandId, ct);

        if (result is null)
            return ServiceResult<BrandDetailDto?>.NotFound("برند یافت نشد.");

        return ServiceResult<BrandDetailDto?>.Success(result);
    }
}