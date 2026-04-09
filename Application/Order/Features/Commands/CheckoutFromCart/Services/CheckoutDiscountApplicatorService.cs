using Domain.Common.ValueObjects;
using Domain.Discount.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public class CheckoutDiscountApplicatorService(IDiscountRepository discountRepository)
    : ICheckoutDiscountApplicatorService
{
    public async Task<ServiceResult<(Money DiscountAmount, Guid? DiscountCodeId)>> ApplyAsync(
        string? discountCode, decimal orderAmount, Guid userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(discountCode))
            return ServiceResult<(Money, Guid?)>.Success((Money.Zero(), null));

        var discount = await discountRepository.GetByCodeAsync(discountCode, ct);
        if (discount is null)
            return ServiceResult<(Money, Guid?)>.NotFound("کد تخفیف یافت نشد.");

        var orderTotal = Money.FromDecimal(orderAmount);
        var validation = discount.ValidateForApplication(orderTotal);
        if (!validation.IsValid)
            return ServiceResult<(Money, Guid?)>.Failure(validation.FailureReason!);

        var discountAmount = discount.CalculateDiscount(orderTotal);
        discount.RecordUsage(UserId.From(userId), Guid.NewGuid().ToString(), discountAmount);

        discountRepository.Update(discount);

        return ServiceResult<(Money, Guid?)>.Success((discountAmount, discount.Id.Value));
    }
}