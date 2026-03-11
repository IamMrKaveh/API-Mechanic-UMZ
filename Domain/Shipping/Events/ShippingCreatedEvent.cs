namespace Domain.Shipping.Events;

public sealed class ShippingCreatedEvent(ShippingId shippingId, string name, decimal baseCost) : DomainEvent
{
    public ShippingId ShippingId { get; } = shippingId;
    public string Name { get; } = name;
    public decimal BaseCost { get; } = baseCost;
}