using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public record CheckoutCartItemsResult(
    List<OrderItemSnapshot> Items,
    decimal Subtotal);