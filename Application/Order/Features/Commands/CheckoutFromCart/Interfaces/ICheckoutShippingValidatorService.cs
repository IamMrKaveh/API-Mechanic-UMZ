namespace Application.Order.Features.Commands.CheckoutFromCart.Interfaces;

public interface ICheckoutShippingValidatorService
{
    Task<ServiceResult<Money>> ValidateAndCalculateCostAsync(
        Guid shippingId,
        decimal orderAmount,
        CancellationToken ct);
}