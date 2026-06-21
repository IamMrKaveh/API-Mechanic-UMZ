using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Queries.GetDiscountInfo;

public class GetDiscountInfoHandler(IDiscountQueryService discountQueryService)
    : IQueryHandler<GetDiscountInfoQuery, DiscountInfoDto>
{
    public async Task<ServiceResult<DiscountInfoDto>> Handle(
        GetDiscountInfoQuery request,
        CancellationToken ct)
    {
        var dto = await discountQueryService.GetDiscountInfoByCodeAsync(request.Code, ct);

        return dto is null
            ? ServiceResult<DiscountInfoDto>.NotFound("کد تخفیف یافت نشد.")
            : ServiceResult<DiscountInfoDto>.Success(dto);
    }
}