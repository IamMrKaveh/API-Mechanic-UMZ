using Application.Discount.Features.Shared;
using Domain.Common.ValueObjects;

namespace Application.Discount.Features.Queries.ValidateDiscount;

public class ValidateDiscountHandler(
    IDiscountQueryService discountQueryService)
    : IRequestHandler<ValidateDiscountQuery, ServiceResult<DiscountValidationResult>>
{
    public async Task<ServiceResult<DiscountValidationResult>> Handle(
        ValidateDiscountQuery request, CancellationToken ct)
    {
        var orderAmount = Money.FromDecimal(request.OrderAmount, request.Currency);

        var result = await discountQueryService.ValidateDiscountAsync(
            request.Code,
            orderAmount,
            request.UserId,
            ct);

        return result.IsValid
            ? ServiceResult<DiscountValidationResult>.Success(result)
            : ServiceResult<DiscountValidationResult>.Failure(result.Error ?? "کد تخفیف نامعتبر است.");
    }
}