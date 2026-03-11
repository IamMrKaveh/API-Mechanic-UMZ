using Domain.User.Entities;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutOrderCreationService
{
    Task<ServiceResult<Domain.Order.Aggregates.Order>> CreateAsync(
        int userId,
        UserAddress address,
        Domain.Shipping.Aggregates.Shipping shippingMethod,
        string idempotencyKey,
        IReadOnlyList<OrderItemSnapshot> orderItemSnapshots,
        CancellationToken ct);
}