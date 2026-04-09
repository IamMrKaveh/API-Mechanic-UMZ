using Domain.Common.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Interfaces;

public interface ICheckoutDiscountApplicatorService
{
    Task<ServiceResult<(Money DiscountAmount, Guid? DiscountCodeId)>> ApplyAsync(
        string? discountCode,
        decimal orderAmount,
        Guid userId,
        CancellationToken ct);
}