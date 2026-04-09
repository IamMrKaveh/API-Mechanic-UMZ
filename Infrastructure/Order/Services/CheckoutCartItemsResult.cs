using Domain.Order.ValueObjects;

namespace Infrastructure.Order.Services;

public record CheckoutCartItemsResult(
    List<OrderItemSnapshot> Items,
    decimal Subtotal);