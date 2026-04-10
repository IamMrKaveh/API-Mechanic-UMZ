using Application.Discount.Features.Shared;
using Domain.Discount.ValueObjects;

namespace Application.Discount.Features.Queries.GetDiscountById;

public class GetDiscountByIdHandler(IDiscountQueryService discountQueryService) : IRequestHandler<GetDiscountByIdQuery, ServiceResult<DiscountCodeDetailDto?>>
{
    private readonly IDiscountQueryService _discountQueryService = discountQueryService;

    public async Task<ServiceResult<DiscountCodeDetailDto?>> Handle(
        GetDiscountByIdQuery request,
        CancellationToken ct)
    {
        var discountCodeId = DiscountCodeId.From(request.Id);
        var dto = await _discountQueryService.GetDetailByIdAsync(discountCodeId, ct);
        return dto is null
            ? ServiceResult<DiscountCodeDetailDto?>.NotFound("یافت نشد")
            : ServiceResult<DiscountCodeDetailDto?>.Success(dto);
    }
}