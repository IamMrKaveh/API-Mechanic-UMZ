using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Queries.ValidateDiscount;

public class ValidateDiscountHandler(
    IDiscountQueryService discountQueryService) : IRequestHandler<ValidateDiscountQuery, ServiceResult<DiscountValidationResult>>
{
    public async Task<ServiceResult<DiscountValidationResult>> Handle(
        ValidateDiscountQuery request, CancellationToken ct)
    {
        var result = await discountQueryService.ValidateDiscountAsync(
            request.Code,
            request.OrderAmount,
            request.UserId,
            ct);

        return result.IsValid
            ? ServiceResult<DiscountValidationResult>.Success(result)
            : ServiceResult<DiscountValidationResult>.Failure(result.Error ?? "کد تخفیف نامعتبر است.");
    }
}