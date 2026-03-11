using Application.Common.Models;

namespace Application.Brand.Features.Queries.GetBrandDetail;

public class GetBrandDetailHandler
    : IRequestHandler<GetBrandDetailQuery, ServiceResult<BrandDetailDto?>>
{
    private readonly IBrandQueryService _brandQueryService;

    public GetBrandDetailHandler(IBrandQueryService brandQueryService)
    {
        _brandQueryService = brandQueryService;
    }

    public async Task<ServiceResult<BrandDetailDto?>> Handle(
        GetBrandDetailQuery request,
        CancellationToken ct)
    {
        var result = await _brandQueryService.GetBrandDetailAsync(request.GroupId, ct);

        if (result == null)
            return ServiceResult<BrandDetailDto?>.Failure("گروه یافت نشد.", 404);

        return ServiceResult<BrandDetailDto?>.Success(result);
    }
}