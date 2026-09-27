using Domain.Cart.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Cart.Events;

public sealed record CartItemRemovedEvent(CartId CartId, VariantId VariantId, int RemovedQuantity) : DomainEvent;
