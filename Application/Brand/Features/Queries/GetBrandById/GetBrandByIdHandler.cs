using Application.Brand.Contracts;
using Application.Brand.Features.Shared;
using Application.Common.Results;

namespace Application.Brand.Features.Queries.GetBrandById;

public class GetBrandByIdHandler(IBrandQueryService brandQueryService) : IRequestHandler<GetBrandByIdQuery, ServiceResult<BrandDetailDto?>>
{
    private readonly IBrandQueryService _brandQueryService = brandQueryService;

    public async Task<ServiceResult<BrandDetailDto?>> Handle(
        GetBrandByIdQuery request,
        CancellationToken ct)
    {
        var result = await _brandQueryService.GetBrandDetailAsync(request.Id, ct);

        if (result == null)
            return ServiceResult<BrandDetailDto?>.NotFound("گروه یافت نشد.");

        return ServiceResult<BrandDetailDto?>.Success(result);
    }
}