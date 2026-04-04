using Application.Common.Results;

namespace Application.Discount.Features.Queries.ValidateDiscount;

public class ValidateDiscountHandler(IDiscountQueryService discountQueryService) : IRequestHandler<ValidateDiscountQuery, ServiceResult<DiscountValidationDto>>
{
    private readonly IDiscountQueryService _discountQueryService = discountQueryService;

    public async Task<ServiceResult<DiscountValidationDto>> Handle(
        ValidateDiscountQuery request,
        CancellationToken ct)
    {
        var result = await _discountQueryService.ValidateDiscountAsync(
            request.Code,
            request.OrderTotal,
            request.UserId,
            ct);

        return ServiceResult<DiscountValidationDto>.Success(result);
    }
}