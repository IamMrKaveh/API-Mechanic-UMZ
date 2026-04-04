using Application.Common.Results;
using Application.Discount.Contracts;
using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Queries.GetDiscountById;

public class GetDiscountByIdHandler(IDiscountQueryService discountQueryService) : IRequestHandler<GetDiscountByIdQuery, ServiceResult<DiscountCodeDetailDto?>>
{
    private readonly IDiscountQueryService _discountQueryService = discountQueryService;

    public async Task<ServiceResult<DiscountCodeDetailDto?>> Handle(
        GetDiscountByIdQuery request,
        CancellationToken ct)
    {
        var dto = await _discountQueryService.GetDetailByIdAsync(request.Id, ct);
        return dto is null
            ? ServiceResult<DiscountCodeDetailDto?>.NotFound("یافت نشد")
            : ServiceResult<DiscountCodeDetailDto?>.Success(dto);
    }
}