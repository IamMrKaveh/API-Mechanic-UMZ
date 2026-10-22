using Domain.Inventory.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Contracts;

/// <summary>
/// One line of an order whose stock has to be reserved.
/// </summary>
public sealed record StockReservationRequest(
    VariantId VariantId,
    StockQuantity Quantity,
    OrderItemId? OrderItemId);
