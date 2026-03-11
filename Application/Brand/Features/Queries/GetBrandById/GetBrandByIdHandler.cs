using Application.Common.Models;

namespace Application.Brand.Features.Queries.GetBrandById;

public class GetBrandByIdHandler
    : IRequestHandler<GetBrandByIdQuery, ServiceResult<BrandDetailDto?>>
{
    private readonly IBrandQueryService _brandQueryService;

    public GetBrandByIdHandler(IBrandQueryService brandQueryService)
    {
        _brandQueryService = brandQueryService;
    }

    public async Task<ServiceResult<BrandDetailDto?>> Handle(
        GetBrandByIdQuery request,
        CancellationToken ct)
    {
        var result = await _brandQueryService.GetBrandDetailAsync(request.Id, ct);

        if (result == null)
            return ServiceResult<BrandDetailDto?>.Failure("گروه یافت نشد.", 404);

        return ServiceResult<BrandDetailDto?>.Success(result);
    }
}