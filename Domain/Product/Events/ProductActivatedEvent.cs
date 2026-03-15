using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed class ProductActivatedEvent(ProductId ProductId) : DomainEvent
{
    public ProductId ProductId { get; } = ProductId;
}