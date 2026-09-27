using Application.Brand.Features.Shared;
using Domain.Brand.ValueObjects;

namespace Application.Brand.Features.Queries.GetBrandDetail;

public class GetBrandDetailHandler(IBrandQueryService brandQueryService)
    : IQueryHandler<GetBrandDetailQuery, BrandDetailDto?>
{
    public async Task<ServiceResult<BrandDetailDto?>> Handle(GetBrandDetailQuery request, CancellationToken ct)
    {
        var brandId = BrandId.From(request.BrandId);
        var fetchResult = await (brandQueryService.GetBrandDetailAsync(brandId, ct)).OrNotFoundAsync("برند یافت نشد.");
        if (fetchResult.IsFailure) return fetchResult.Error;
        var result = fetchResult.Value;

        return ServiceResult<BrandDetailDto?>.Success(result);
    }
}