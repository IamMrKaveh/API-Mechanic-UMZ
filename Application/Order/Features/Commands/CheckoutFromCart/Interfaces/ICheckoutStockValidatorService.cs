using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Interfaces;

public interface ICheckoutStockValidatorService
{
    Task<ServiceResult> ValidateAsync(IReadOnlyCollection<OrderItemSnapshot> items, CancellationToken ct);
}