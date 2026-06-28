using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Interfaces;

public interface ICheckoutShippingValidatorService
{
    Task<ServiceResult<Money>> ValidateAndCalculateCostAsync(
        Guid shippingId,
        decimal orderAmount,
        IReadOnlyCollection<OrderItemSnapshot> items,
        CancellationToken ct);
}