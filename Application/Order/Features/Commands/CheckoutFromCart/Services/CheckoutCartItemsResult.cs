using Domain.Variant.Aggregates;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public sealed class CheckoutCartItemsResult
{
    public IReadOnlyList<OrderItemSnapshot> OrderItemSnapshots { get; init; } = [];
    public IReadOnlyList<(ProductVariant Variant, int Quantity)> VariantItems { get; init; } = [];
}