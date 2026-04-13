namespace Application.Order.Features.Commands.CheckoutFromCart.Interfaces;

public interface ICheckoutDiscountApplicatorService
{
    Task<ServiceResult<(Money DiscountAmount, Guid? DiscountCodeId)>> ApplyAsync(
    string? discountCode,
    Money orderAmount,
    Guid userId,
    CancellationToken ct);
}