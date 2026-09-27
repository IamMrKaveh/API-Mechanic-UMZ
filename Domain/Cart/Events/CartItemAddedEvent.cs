using Domain.Cart.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Cart.Events;

public sealed record CartItemAddedEvent(
    CartId CartId,
    VariantId VariantId,
    ProductId ProductId,
    ProductName ProductName,
    int Quantity,
    decimal UnitPrice) : DomainEvent;
