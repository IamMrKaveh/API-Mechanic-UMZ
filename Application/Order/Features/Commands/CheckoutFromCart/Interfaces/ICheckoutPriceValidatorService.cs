using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Interfaces;

public interface ICheckoutPriceValidatorService
{
    Task<ServiceResult> ValidateAsync(IReadOnlyCollection<OrderItemSnapshot> items, CancellationToken ct);
}