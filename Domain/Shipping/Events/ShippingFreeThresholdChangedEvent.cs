using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Events;

public sealed class ShippingFreeThresholdChangedEvent(ShippingId shippingId, bool isEnabled, decimal? thresholdAmount) : DomainEvent
{
    public ShippingId ShippingId { get; } = shippingId;
    public bool IsEnabled { get; } = isEnabled;
    public decimal? ThresholdAmount { get; } = thresholdAmount;
}