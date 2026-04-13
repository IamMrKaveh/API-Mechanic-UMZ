using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Interfaces;

public sealed record CheckoutCartItemsResult(
    IReadOnlyList<OrderItemSnapshot> Items,
    decimal SubTotal);

public interface ICheckoutCartItemBuilderService
{
    Task<ServiceResult<CheckoutCartItemsResult>> BuildAsync(Guid cartId, Guid userId, CancellationToken ct);
}