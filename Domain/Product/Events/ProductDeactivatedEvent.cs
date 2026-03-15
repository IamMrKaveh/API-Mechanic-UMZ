using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed class ProductDeactivatedEvent(ProductId ProductId) : DomainEvent
{
    public ProductId ProductId { get; } = ProductId;
}