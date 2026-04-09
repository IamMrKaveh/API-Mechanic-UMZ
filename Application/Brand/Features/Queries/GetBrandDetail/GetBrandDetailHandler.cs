using Application.Brand.Features.Shared;

namespace Application.Brand.Features.Queries.GetBrandDetail;

public class GetBrandDetailHandler(IBrandQueryService brandQueryService) : IRequestHandler<GetBrandDetailQuery, ServiceResult<BrandDetailDto?>>
{
    private readonly IBrandQueryService _brandQueryService = brandQueryService;

    public async Task<ServiceResult<BrandDetailDto?>> Handle(
        GetBrandDetailQuery request,
        CancellationToken ct)
    {
        var result = await _brandQueryService.GetBrandDetailAsync(request.BrandId, ct);

        if (result == null)
            return ServiceResult<BrandDetailDto?>.NotFound("گروه یافت نشد.");

        return ServiceResult<BrandDetailDto?>.Success(result);
    }
}