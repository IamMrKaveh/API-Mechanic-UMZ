using Application.Common.Models;

namespace Application.Discount.Features.Queries.GetDiscountById;

public class GetDiscountByIdHandler(IDiscountQueryService discountQueryService) : IRequestHandler<GetDiscountByIdQuery, ServiceResult<DiscountCodeDetailDto?>>
{
    private readonly IDiscountQueryService _discountQueryService = discountQueryService;

    public async Task<ServiceResult<DiscountCodeDetailDto?>> Handle(
        GetDiscountByIdQuery request,
        CancellationToken ct)
    {
        var dto = await _discountQueryService.GetDetailByIdAsync(request.Id, ct);
        return dto == null
            ? ServiceResult<DiscountCodeDetailDto?>.Failure("یافت نشد")
            : ServiceResult<DiscountCodeDetailDto?>.Success(dto);
    }
}