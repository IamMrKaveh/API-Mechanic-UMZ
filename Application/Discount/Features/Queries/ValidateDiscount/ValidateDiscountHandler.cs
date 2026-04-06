using Application.Common.Results;
using Application.Discount.Contracts;
using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Queries.ValidateDiscount;

public class ValidateDiscountHandler(
    IDiscountService discountService) : IRequestHandler<ValidateDiscountQuery, ServiceResult<DiscountValidationResult>>
{
    private readonly IDiscountService _discountService = discountService;

    public async Task<ServiceResult<DiscountValidationResult>> Handle(
        ValidateDiscountQuery request,
        CancellationToken ct)
    {
        return await _discountService.ValidateAndApplyDiscountAsync(
            request.Code,
            request.OrderAmount,
            request.UserId,
            ct);
    }
}