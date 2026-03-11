namespace Domain.Shipping.Events;

public sealed class ShippingSetAsDefaultEvent : DomainEvent
{
    public ShippingId ShippingId { get; }
    public string Name { get; }

    public ShippingSetAsDefaultEvent(ShippingId shippingId, string name)
    {
        ShippingId = shippingId;
        Name = name;
    }
}