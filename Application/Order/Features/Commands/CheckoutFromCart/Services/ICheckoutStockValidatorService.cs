using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutStockValidatorService
{
    Task<ServiceResult> ValidateAsync(List<OrderItemSnapshot> items, CancellationToken ct);
}