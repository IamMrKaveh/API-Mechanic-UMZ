using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Queries.ValidateDiscount;

public class ValidateDiscountHandler(
    IDiscountQueryService discountQueryService,
    ICurrentUserService currentUserService)
    : IRequestHandler<ValidateDiscountQuery, ServiceResult<DiscountValidationResult>>
{
    public async Task<ServiceResult<DiscountValidationResult>> Handle(
        ValidateDiscountQuery request, CancellationToken ct)
    {
        var orderAmount = Money.FromDecimal(request.OrderAmount, request.Currency);

        var result = await discountQueryService.ValidateDiscountAsync(
            request.Code,
            orderAmount,
            currentUserService.UserId.Value,
            ct);

        return result.IsValid
            ? ServiceResult<DiscountValidationResult>.Success(result)
            : ServiceResult<DiscountValidationResult>.Failure(result.Error ?? "کد تخفیف نامعتبر است.");
    }
}