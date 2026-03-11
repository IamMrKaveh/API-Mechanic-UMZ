namespace Domain.Shipping.Events;

public sealed class ShippingActivatedEvent(ShippingId shippingId, string name) : DomainEvent
{
    public ShippingId ShippingId { get; } = shippingId;
    public string Name { get; } = name;
}