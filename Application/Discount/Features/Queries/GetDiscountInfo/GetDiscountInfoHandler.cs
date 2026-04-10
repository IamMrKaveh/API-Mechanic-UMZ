using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Queries.GetDiscountInfo;

public class GetDiscountInfoHandler(IDiscountQueryService discountQueryService) : IRequestHandler<GetDiscountInfoQuery, ServiceResult<DiscountInfoDto>>
{
    private readonly IDiscountQueryService _discountQueryService = discountQueryService;

    public async Task<ServiceResult<DiscountInfoDto>> Handle(
        GetDiscountInfoQuery request,
        CancellationToken ct)
    {
        var dto = await _discountQueryService.GetDiscountInfoByCodeAsync(request.Code, ct);

        return dto == null
            ? ServiceResult<DiscountInfoDto>.NotFound("کد تخفیف یافت نشد.")
            : ServiceResult<DiscountInfoDto>.Success(dto);
    }
}