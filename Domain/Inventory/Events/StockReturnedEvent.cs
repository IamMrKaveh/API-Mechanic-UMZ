using Domain.Order.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed record StockReturnedEvent(VariantId VariantId, OrderId OrderId, int Quantity) : DomainEvent;
